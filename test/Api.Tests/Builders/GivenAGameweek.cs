namespace Api.Tests.Builders;

using Api.Controllers;
using Domain.Model;
using Domain.Predicting;

/// <summary>Builds a controller over a stubbed gameweek predictor.</summary>
internal sealed class GivenAGameweek
{
    private readonly List<FixturePrediction> fixtures = [];

    public static GivenAGameweek WithNoFixtures() => new();

    public static GivenAGameweek With(
        string home, string away, int hour, params (int Home, int Away, double Confidence)[] predictions)
        => new GivenAGameweek().And(home, away, hour, predictions);

    public GivenAGameweek And(
        string home, string away, int hour, params (int Home, int Away, double Confidence)[] predictions)
    {
        fixtures.Add(new FixturePrediction(
            new Fixture(
                Id: $"espn:{hour}",
                Gameweek: 5,
                KickoffUtc: new DateTimeOffset(2026, 9, 19, hour, 0, 0, TimeSpan.Zero),
                HomeTeam: home,
                AwayTeam: away),
            [.. predictions.Select(p => new Prediction(p.Home, p.Away, p.Confidence))]));

        return this;
    }

    public StubGameweekPredictor BuildPredictor() => new([.. fixtures]);

    public GameweeksController Build() => new(BuildPredictor());
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
