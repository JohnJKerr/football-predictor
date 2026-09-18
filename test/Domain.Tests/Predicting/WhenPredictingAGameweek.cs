namespace Domain.Tests.Predicting;

using Domain.Model;
using Domain.Predicting;
using Domain.Schedule;
using Domain.Tests.Schedule;

public class WhenPredictingAGameweek
{
    /// <summary>Predicts a scoreline derived from the fixture id, so each fixture is distinguishable.</summary>
    private sealed class RecordingMatchPredictor : IMatchPredictor
    {
        public List<string> Asked { get; } = [];

        public int InFlight { get; private set; }

        public int PeakInFlight { get; private set; }

        public async Task<IReadOnlyList<Prediction>> PredictAsync(
            Fixture fixture, CancellationToken cancellationToken = default)
        {
            InFlight++;
            PeakInFlight = Math.Max(PeakInFlight, InFlight);
            Asked.Add(fixture.Id);

            await Task.Yield();

            InFlight--;
            var goals = int.Parse(fixture.Id.Split('-')[1]);
            return [new Prediction(goals, 0, 0.5)];
        }
    }

    private static List<FixtureListing> Season(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new FixtureListing(
            $"match-{i}",
            new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero).AddHours(i),
            $"Home {i}",
            $"Away {i}"))];

    private static GameweekPredictor Build(IMatchPredictor predictor, int fixtureCount = 20) =>
        new(new GameweekSchedule(new StubFixtureSource(Season(fixtureCount))), predictor);

    [Fact]
    public async Task Every_fixture_in_the_gameweek_is_predicted()
    {
        var predictor = new RecordingMatchPredictor();

        var results = await Build(predictor).PredictAsync(2);

        Assert.Equal(10, results.Count);
        Assert.Equal(
            Enumerable.Range(10, 10).Select(i => $"match-{i}"),
            results.Select(r => r.Fixture.Id));
    }

    [Fact]
    public async Task Each_fixture_is_paired_with_its_own_predictions()
    {
        var results = await Build(new RecordingMatchPredictor()).PredictAsync(1);

        Assert.All(results, result =>
        {
            var expectedGoals = int.Parse(result.Fixture.Id.Split('-')[1]);
            Assert.Equal(expectedGoals, Assert.Single(result.Predictions).HomeScore);
        });
    }

    [Fact]
    public async Task Fixtures_are_predicted_one_at_a_time_in_kickoff_order()
    {
        var predictor = new RecordingMatchPredictor();

        await Build(predictor).PredictAsync(1);

        Assert.Equal(1, predictor.PeakInFlight);
        Assert.Equal(
            Enumerable.Range(0, 10).Select(i => $"match-{i}"),
            predictor.Asked);
    }

    [Fact]
    public async Task A_gameweek_outside_the_season_predicts_nothing()
    {
        var predictor = new RecordingMatchPredictor();

        var results = await Build(predictor).PredictAsync(99);

        Assert.Empty(results);
        Assert.Empty(predictor.Asked);
    }
}
