namespace Domain.Tests.Predicting;

using Domain.Model;
using Domain.Predicting;

internal sealed class StubJevPredictor(ScorelineProbabilities probabilities) : IJevPredictor
{
    public Fixture? Asked { get; private set; }

    public Task<ScorelineProbabilities> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        Asked = fixture;
        return Task.FromResult(probabilities);
    }
}
