namespace Domain.Tests.Predicting;

using Domain.Predicting;
using Domain.Schedule;
using Domain.Tests.Builders;

public class WhenPredictingAGameweek
{
    private static GameweekPredictor Predictor(RecordingMatchPredictor matches, int fixtures = 20)
        => new(new GameweekSchedule(GivenASeason.Of(fixtures).Build()), matches);

    [Fact]
    public async Task Every_fixture_in_the_gameweek_is_predicted()
    {
        // Arrange
        var matches = new RecordingMatchPredictor();

        // Act
        var results = await Predictor(matches).PredictAsync(2);

        // Assert
        Assert.Equal(
            Enumerable.Range(10, 10).Select(i => $"match-{i}"),
            results.Select(r => r.Fixture.Id));
    }

    [Fact]
    public async Task Each_fixture_is_paired_with_its_own_predictions()
    {
        // Arrange
        var matches = new RecordingMatchPredictor();

        // Act
        var results = await Predictor(matches).PredictAsync(1);

        // Assert
        Assert.Equal(
            results.Select(r => int.Parse(r.Fixture.Id.Split('-')[1])),
            results.Select(r => r.Predictions[0].HomeScore));
    }

    [Fact]
    public async Task Fixtures_are_predicted_one_at_a_time()
    {
        // Arrange
        var matches = new RecordingMatchPredictor();

        // Act
        await Predictor(matches).PredictAsync(1);

        // Assert
        Assert.Equal(1, matches.PeakInFlight);
    }

    [Fact]
    public async Task Fixtures_are_predicted_in_kickoff_order()
    {
        // Arrange
        var matches = new RecordingMatchPredictor();

        // Act
        await Predictor(matches).PredictAsync(1);

        // Assert
        Assert.Equal(Enumerable.Range(0, 10).Select(i => $"match-{i}"), matches.Asked);
    }

    [Fact]
    public async Task A_gameweek_outside_the_season_predicts_nothing()
    {
        // Arrange
        var matches = new RecordingMatchPredictor();

        // Act
        var results = await Predictor(matches).PredictAsync(99);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task A_gameweek_outside_the_season_asks_Jev_nothing()
    {
        // Arrange
        var matches = new RecordingMatchPredictor();

        // Act
        await Predictor(matches).PredictAsync(99);

        // Assert
        Assert.Empty(matches.Asked);
    }
}
