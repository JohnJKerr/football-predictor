namespace Api.Tests.Builders;

using Api.Controllers;
using Domain.Model;
using Domain.Predicting;

/// <summary>Builds a controller over a stubbed gameweek predictor.</summary>
internal sealed class GivenAGameweek
{
    private readonly List<(string Home, string Away, int Hour, (int Home, int Away, double Confidence)[] Scores)> fixtures = [];
    private Dictionary<Outcome, double> outcomes = [];
    private double outcomeConfidence;
    private double overGoals;
    private double bothToScore;

    public static GivenAGameweek WithNoFixtures() => new();

    public static GivenAGameweek With(
        string home, string away, int hour, params (int Home, int Away, double Confidence)[] scores)
        => new GivenAGameweek().And(home, away, hour, scores);

    public GivenAGameweek And(
        string home, string away, int hour, params (int Home, int Away, double Confidence)[] scores)
    {
        fixtures.Add((home, away, hour, scores));
        return this;
    }

    /// <summary>Applies to every fixture, whenever it is called.</summary>
    public GivenAGameweek Forecasting(
        double home, double draw, double away, double confidence, double over, double bothScore)
    {
        outcomes = new Dictionary<Outcome, double>
        {
            [Outcome.HomeWin] = home,
            [Outcome.Draw] = draw,
            [Outcome.AwayWin] = away,
        };
        outcomeConfidence = confidence;
        overGoals = over;
        bothToScore = bothScore;
        return this;
    }

    public StubGameweekPredictor BuildPredictor() => new([.. fixtures.Select(Predicted)]);

    public GameweeksController Build() => new(BuildPredictor());

    private FixturePrediction Predicted(
        (string Home, string Away, int Hour, (int Home, int Away, double Confidence)[] Scores) fixture)
        => new(
            new Fixture(
                Id: $"espn:{fixture.Hour}",
                Gameweek: 5,
                KickoffUtc: new DateTimeOffset(2026, 9, 19, fixture.Hour, 0, 0, TimeSpan.Zero),
                HomeTeam: fixture.Home,
                AwayTeam: fixture.Away),
            new MatchPrediction(
                [.. fixture.Scores.Select(s => new Prediction(s.Home, s.Away, s.Confidence))],
                new OutcomeProbabilities(outcomes, outcomeConfidence),
                overGoals,
                bothToScore));
}

internal sealed class StubGameweekPredictor(FixturePrediction[] predictions) : IGameweekPredictor
{
    public int? Asked { get; private set; }

    public Task<IReadOnlyList<FixturePrediction>> PredictAsync(
        int gameweek, CancellationToken cancellationToken = default)
    {
        Asked = gameweek;
        return Task.FromResult<IReadOnlyList<FixturePrediction>>(predictions);
    }
}
