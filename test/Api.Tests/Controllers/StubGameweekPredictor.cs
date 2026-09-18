namespace Api.Tests.Controllers;

using Domain.Predicting;

internal sealed class StubGameweekPredictor(params FixturePrediction[] predictions) : IGameweekPredictor
{
    public int? Asked { get; private set; }

    public Task<IReadOnlyList<FixturePrediction>> PredictAsync(
        int gameweek, CancellationToken cancellationToken = default)
    {
        Asked = gameweek;
        return Task.FromResult<IReadOnlyList<FixturePrediction>>(predictions);
    }
}
