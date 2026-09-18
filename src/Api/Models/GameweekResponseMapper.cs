namespace Api.Models;

using Domain.Model;
using Domain.Predicting;

internal static class GameweekResponseMapper
{
    public static GameweekResponse ToResponse(
        this IReadOnlyList<FixturePrediction> predictions,
        int gameweek,
        IReadOnlyList<string>? state = null) =>
        new(gameweek, [.. predictions.Select(ToResponse)], state);

    private static FixturePredictionResponse ToResponse(FixturePrediction prediction) => new(
        prediction.Fixture.Id,
        prediction.Fixture.KickoffUtc,
        prediction.Fixture.HomeTeam,
        prediction.Fixture.AwayTeam,
        prediction.MostLikely is { } best
            ? new ScoreResponse(best.HomeScore, best.AwayScore, best.Confidence)
            : null,
        ToResponse(prediction.Prediction.Outcome),
        prediction.Prediction.OverTwoAndAHalfGoals,
        prediction.Prediction.BothTeamsToScore);

    private static OutcomeResponse? ToResponse(OutcomeProbabilities outcome) =>
        outcome.ByOutcome.Count == 0
            ? null
            : new OutcomeResponse(
                outcome.MostLikely.ToString(),
                outcome.ProbabilityOf(Outcome.HomeWin),
                outcome.ProbabilityOf(Outcome.Draw),
                outcome.ProbabilityOf(Outcome.AwayWin),
                outcome.Confidence);
}
