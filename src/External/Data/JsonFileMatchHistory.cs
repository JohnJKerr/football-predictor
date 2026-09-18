namespace External.Data;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.History;

/// <summary>
/// Reads completed matches from the on-disk results dataset, keeping only the fields that
/// bear on a future scoreline. Lineups, substitutions, commentary and goal events are the
/// bulk of the file and are dropped here rather than travelling to Jev.
/// </summary>
public sealed class JsonFileMatchHistory(string path) : IMatchHistory
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IReadOnlyList<CompletedMatch>? cached;

    public async Task<IReadOnlyList<CompletedMatch>> GetCompletedAsync(
        CancellationToken cancellationToken = default)
    {
        if (cached is not null) return cached;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cached is not null) return cached;

            await using var stream = File.OpenRead(path);
            var document = await JsonSerializer.DeserializeAsync<ResultsFile>(stream, Options, cancellationToken)
                           ?? throw new InvalidDataException($"'{path}' is not a results file.");

            return cached = [.. document.Matches.Select(ToCompletedMatch)];
        }
        finally
        {
            gate.Release();
        }
    }

    private static CompletedMatch ToCompletedMatch(MatchRecord match) => new(
        DateTimeOffset.Parse(match.KickoffUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
        ToPerformance(match.Teams.Home),
        ToPerformance(match.Teams.Away));

    private static TeamPerformance ToPerformance(TeamRecord team) => new(
        team.Name,
        team.Goals,
        team.LeaguePositionBeforeGame,
        team.MatchStats is null
            ? null
            : new MatchStats(
                team.MatchStats.Shots,
                team.MatchStats.ShotsOnTarget,
                team.MatchStats.ShotsOffTarget,
                team.MatchStats.BlockedShots,
                team.MatchStats.Xg,
                team.MatchStats.PossessionPercent));

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private sealed record ResultsFile(
        [property: JsonPropertyName("matches")] IReadOnlyList<MatchRecord> Matches);

    private sealed record MatchRecord(string KickoffUtc, TeamsRecord Teams);

    private sealed record TeamsRecord(TeamRecord Home, TeamRecord Away);

    private sealed record TeamRecord(
        string Name,
        int Goals,
        int? LeaguePositionBeforeGame,
        StatsRecord? MatchStats);

    private sealed record StatsRecord(
        int Shots,
        int ShotsOnTarget,
        int ShotsOffTarget,
        int BlockedShots,
        double? Xg,
        double? PossessionPercent);
}
