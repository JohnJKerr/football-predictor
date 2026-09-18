namespace Domain.Predicting;

using Domain.Schedule;

/// <summary>Predicts every fixture in a gameweek, in kickoff order.</summary>
public sealed class GameweekPredictor(IGameweekSchedule schedule, IMatchPredictor predictor)
    : IGameweekPredictor
{
    public async Task<IReadOnlyList<FixturePrediction>> PredictAsync(
        int gameweek, CancellationToken cancellationToken = default)
    {
        var fixtures = await schedule.GetGameweekAsync(gameweek, cancellationToken);
        if (fixtures.Count == 0) return [];

        // One fixture at a time: Jev answers quickly, and ten calls at once would only
        // put the gameweek at risk of a rate limit for no real gain.
        var predictions = new List<FixturePrediction>(fixtures.Count);

        foreach (var fixture in fixtures)
        {
            predictions.Add(new FixturePrediction(
                fixture,
                await predictor.PredictAsync(fixture, cancellationToken)));
        }

        return predictions;
    }
}
