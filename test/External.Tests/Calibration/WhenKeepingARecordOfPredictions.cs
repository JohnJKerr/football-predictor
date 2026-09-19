namespace External.Tests.Calibration;

using Domain.Calibration;
using Domain.Model;
using Domain.Predicting;
using External.Calibration;

public class WhenKeepingARecordOfPredictions
{
    private static JsonFilePredictionLog InAFreshDirectory() =>
        new(Path.Combine(Path.GetTempPath(), "football-predictor-tests", Guid.NewGuid().ToString("n")));

    private static IReadOnlyList<FixturePrediction> APrediction(
        string home = "Arsenal", string away = "Chelsea") =>
        [new FixturePrediction(
            new Fixture("espn:1", 4, new DateTimeOffset(2026, 9, 12, 14, 0, 0, TimeSpan.Zero), home, away),
            new MatchPrediction(
                [new Prediction(2, 1, 0.31), new Prediction(1, 1, 0.22)],
                new OutcomeProbabilities(
                    new Dictionary<Outcome, double>
                    {
                        [Outcome.HomeWin] = 0.62, [Outcome.Draw] = 0.23, [Outcome.AwayWin] = 0.15,
                    }, 0.44),
                0.58, 0.61))];

    [Fact]
    public async Task A_recorded_prediction_can_be_read_back()
    {
        // Arrange
        var log = InAFreshDirectory();
        await log.RecordAsync(4, APrediction());

        // Act
        var recorded = await log.ForGameweekAsync(4);

        // Assert
        Assert.Equal(("Arsenal", "Chelsea"), (Assert.Single(recorded).HomeTeam, recorded[0].AwayTeam));
    }

    [Fact]
    public async Task What_Jev_said_about_the_outcome_survives_the_round_trip()
    {
        // Arrange
        var log = InAFreshDirectory();
        await log.RecordAsync(4, APrediction());

        // Act
        var recorded = await log.ForGameweekAsync(4);

        // Assert
        Assert.Equal(0.62, Assert.Single(recorded).Outcome.ProbabilityOf(Outcome.HomeWin));
    }

    [Fact]
    public async Task The_likeliest_scoreline_survives_the_round_trip()
    {
        // Arrange
        var log = InAFreshDirectory();
        await log.RecordAsync(4, APrediction());

        // Act
        var recorded = await log.ForGameweekAsync(4);

        // Assert
        Assert.Equal(new Prediction(2, 1, 0.31), Assert.Single(recorded).Scoreline);
    }

    [Fact]
    public async Task A_gameweek_never_predicted_has_no_record()
    {
        // Arrange
        var log = InAFreshDirectory();

        // Act
        var recorded = await log.ForGameweekAsync(9);

        // Assert
        Assert.Empty(recorded);
    }

    [Fact]
    public async Task Predicting_a_gameweek_again_replaces_what_was_there()
    {
        // Arrange
        var log = InAFreshDirectory();
        await log.RecordAsync(4, APrediction("Arsenal", "Chelsea"));

        // Act
        await log.RecordAsync(4, APrediction("Everton", "Fulham"));

        // Assert
        Assert.Equal("Everton", Assert.Single(await log.ForGameweekAsync(4)).HomeTeam);
    }
}
