namespace Domain.Predicting;

using Domain.Model;

/// <summary>
/// Turns Jev's probability distribution into the candidate results for a fixture,
/// most likely first. Confidence is Jev's own probability for that exact scoreline.
/// </summary>
public sealed class MatchPredictor(IJevPredictor jev) : IMatchPredictor
{
    public async Task<IReadOnlyList<Prediction>> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var forecast = await jev.PredictAsync(fixture, cancellationToken);

        // The "other" bucket is deliberately dropped: it carries probability mass but
        // names no specific result, so there is no scoreline we could predict from it.
        return
        [
            .. forecast.Scorelines.ByScoreline
                .Where(entry => entry.Value > 0)
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key.HomeScore)
                .ThenBy(entry => entry.Key.AwayScore)
                .Select(entry => new Prediction(entry.Key.HomeScore, entry.Key.AwayScore, entry.Value))
        ];
    }
}
