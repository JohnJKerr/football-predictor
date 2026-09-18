namespace Domain.Tests.Builders;

using Domain.Model;
using Domain.Predicting;

/// <summary>A stubbed <see cref="IJevPredictor"/> returning a chosen distribution.</summary>
internal sealed class GivenJev
{
    private readonly Dictionary<Scoreline, double> scorelines = [];
    private double other;
    private double confidence = 0.8;

    public static GivenJev Returning(params (int Home, int Away, double Probability)[] scorelines)
    {
        var builder = new GivenJev();
        foreach (var (home, away, probability) in scorelines)
        {
            builder.scorelines[new Scoreline(home, away)] = probability;
        }

        return builder;
    }

    public GivenJev AndInTheCatchAll(double probability)
    {
        other = probability;
        return this;
    }

    public StubJevPredictor Build() => new(new ScorelineProbabilities(scorelines, other, confidence));
}

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
