namespace Api.Models;

using Domain.Predicting;

internal static class GameweekResponseMapper
{
    public static GameweekResponse ToResponse(this IReadOnlyList<FixturePrediction> predictions, int gameweek) =>
        new(gameweek, [.. predictions.Select(ToResponse)]);

    private static FixturePredictionResponse ToResponse(FixturePrediction prediction) => new(
        prediction.Fixture.Id,
        prediction.Fixture.KickoffUtc,
        prediction.Fixture.HomeTeam,
        prediction.Fixture.AwayTeam,
        prediction.MostLikely is { } best
            ? new ScoreResponse(best.HomeScore, best.AwayScore, best.Confidence)
            : null);
}
