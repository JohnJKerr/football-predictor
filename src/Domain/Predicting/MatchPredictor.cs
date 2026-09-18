namespace Domain.Predicting;

using Domain.Model;

/// <summary>
/// Turns Jev's answers into what we will say about a fixture. Scorelines are ranked by the
/// probability Jev gave them; the narrower judgements pass through as asked.
/// </summary>
public sealed class MatchPredictor(IJevPredictor jev) : IMatchPredictor
{
    public async Task<MatchPrediction> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var forecast = await jev.PredictAsync(fixture, cancellationToken);

        return new MatchPrediction(
            Rank(forecast.Scorelines),
            forecast.Outcome,
            forecast.OverTwoAndAHalfGoals,
            forecast.BothTeamsToScore);
    }

    private static IReadOnlyList<Prediction> Rank(ScorelineProbabilities distribution) =>
    [
        // The "other" bucket is deliberately dropped: it carries probability mass but
        // names no specific result, so there is no scoreline we could predict from it.
        .. distribution.ByScoreline
            .Where(entry => entry.Value > 0)
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key.HomeScore)
            .ThenBy(entry => entry.Key.AwayScore)
            .Select(entry => new Prediction(entry.Key.HomeScore, entry.Key.AwayScore, entry.Value))
    ];
}
