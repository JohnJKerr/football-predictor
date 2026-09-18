namespace Api.Models;

using Domain.Model;
using Domain.Predicting;

internal static class GameweekResponseMapper
{
    /// <summary>
    /// How many candidate scorelines to report by default. Enough to show that no single
    /// result is likely, without listing a grid of near-zero tails.
    /// </summary>
    public const int DefaultScorelines = 5;

    public static GameweekResponse ToResponse(
        this IReadOnlyList<FixturePrediction> predictions,
        int gameweek,
        int scorelines = DefaultScorelines,
        IReadOnlyList<string>? state = null) =>
        new(gameweek, [.. predictions.Select(p => ToResponse(p, scorelines))], state);

    private static FixturePredictionResponse ToResponse(FixturePrediction prediction, int scorelines)
    {
        var shown = prediction.Prediction.Scorelines.Take(scorelines).ToList();

        return new FixturePredictionResponse(
            prediction.Fixture.Id,
            prediction.Fixture.KickoffUtc,
            prediction.Fixture.HomeTeam,
            prediction.Fixture.AwayTeam,
            [.. shown.Select(s => new ScoreResponse(s.HomeScore, s.AwayScore, s.Confidence))],
            // Everything Jev did not put on a shown scoreline, the catch-all bucket included.
            Math.Round(Math.Max(0, 1 - shown.Sum(s => s.Confidence)), 4),
            ToResponse(prediction.Prediction.Outcome),
            prediction.Prediction.OverTwoAndAHalfGoals,
            prediction.Prediction.BothTeamsToScore);
    }

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
