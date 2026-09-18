namespace Domain.Tests.Builders;

using Domain.Model;
using Domain.Predicting;

/// <summary>A stubbed <see cref="IJevPredictor"/> returning a chosen distribution.</summary>
internal sealed class GivenJev
{
    private readonly Dictionary<Scoreline, double> scorelines = [];
    private readonly Dictionary<Outcome, double> outcomes = [];
    private double other;
    private double confidence = 0.8;
    private double outcomeConfidence;
    private double overGoals;
    private double bothToScore;

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

    public GivenJev AndOutcome(double home, double draw, double away, double confidence)
    {
        outcomes[Outcome.HomeWin] = home;
        outcomes[Outcome.Draw] = draw;
        outcomes[Outcome.AwayWin] = away;
        outcomeConfidence = confidence;
        return this;
    }

    public GivenJev AndGoals(double overTwoAndAHalf, double bothTeamsToScore)
    {
        overGoals = overTwoAndAHalf;
        bothToScore = bothTeamsToScore;
        return this;
    }

    public StubJevPredictor Build() => new(new MatchForecast(
        new ScorelineProbabilities(scorelines, other, confidence),
        new OutcomeProbabilities(outcomes, outcomeConfidence),
        overGoals,
        bothToScore));
}

internal sealed class StubJevPredictor(MatchForecast forecast) : IJevPredictor
{
    public Fixture? Asked { get; private set; }

    public Task<MatchForecast> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        Asked = fixture;
        return Task.FromResult(forecast);
    }
}
