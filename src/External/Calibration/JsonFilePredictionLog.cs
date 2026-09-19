namespace External.Calibration;

using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Calibration;
using Domain.Model;
using Domain.Predicting;

/// <summary>
/// Keeps each gameweek's predictions on disk so they can be shown back to Jev once the
/// results are in. One file per gameweek, rewritten whenever that gameweek is predicted
/// again: only the latest prediction for a gameweek is of interest.
/// </summary>
public sealed class JsonFilePredictionLog(string directory) : IPredictionLog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public async Task RecordAsync(
        int gameweek, IReadOnlyList<FixturePrediction> predictions, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directory);

        var file = new LogFile(gameweek,
        [
            .. predictions.Select(p => new Entry(
                p.Fixture.HomeTeam,
                p.Fixture.AwayTeam,
                new OutcomeEntry(
                    p.Prediction.Outcome.ProbabilityOf(Outcome.HomeWin),
                    p.Prediction.Outcome.ProbabilityOf(Outcome.Draw),
                    p.Prediction.Outcome.ProbabilityOf(Outcome.AwayWin),
                    p.Prediction.Outcome.Confidence),
                p.MostLikely is { } best
                    ? new ScoreEntry(best.HomeScore, best.AwayScore, best.Confidence)
                    : null))
        ]);

        // Written beside and moved into place, so a failed write cannot leave half a file.
        var path = PathFor(gameweek);
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(file, Options), cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    public async Task<IReadOnlyList<LoggedPrediction>> ForGameweekAsync(
        int gameweek, CancellationToken cancellationToken = default)
    {
        var path = PathFor(gameweek);
        if (!File.Exists(path)) return [];

        await using var stream = File.OpenRead(path);
        var file = await JsonSerializer.DeserializeAsync<LogFile>(stream, Options, cancellationToken);
        if (file is null) return [];

        return
        [
            .. file.Predictions.Select(e => new LoggedPrediction(
                file.Gameweek,
                e.HomeTeam,
                e.AwayTeam,
                new OutcomeProbabilities(
                    new Dictionary<Outcome, double>
                    {
                        [Outcome.HomeWin] = e.Outcome.HomeWin,
                        [Outcome.Draw] = e.Outcome.Draw,
                        [Outcome.AwayWin] = e.Outcome.AwayWin,
                    },
                    e.Outcome.Confidence),
                e.Scoreline is { } s ? new Prediction(s.Home, s.Away, s.Confidence) : null))
        ];
    }

    private string PathFor(int gameweek) => Path.Combine(directory, $"gameweek-{gameweek}.json");

    private sealed record LogFile(
        int Gameweek,
        [property: JsonPropertyName("predictions")] IReadOnlyList<Entry> Predictions);

    private sealed record Entry(string HomeTeam, string AwayTeam, OutcomeEntry Outcome, ScoreEntry? Scoreline);

    private sealed record OutcomeEntry(double HomeWin, double Draw, double AwayWin, double Confidence);

    private sealed record ScoreEntry(int Home, int Away, double Confidence);
}
