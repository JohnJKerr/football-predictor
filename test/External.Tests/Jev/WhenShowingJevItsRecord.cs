namespace External.Tests.Jev;

using Domain.Calibration;
using Domain.Model;
using Domain.Predicting;
using External.Tests.Builders;

public class WhenShowingJevItsRecord
{
    private static PredictionOutcome Said(
        string home, string away, double h, double d, double a, int actualHome, int actualAway) =>
        new(4, home, away,
            new OutcomeProbabilities(
                new Dictionary<Outcome, double>
                {
                    [Outcome.HomeWin] = h, [Outcome.Draw] = d, [Outcome.AwayWin] = a,
                }, 0.5),
            new Prediction(2, 1, 0.3),
            actualHome, actualAway);

    [Fact]
    public async Task The_record_travels_with_the_question()
    {
        // Arrange
        var jev = GivenJev.WithRecord(Said("Arsenal", "Chelsea", 0.95, 0.04, 0.01, 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("Arsenal v Chelsea", (string?)jev.Calibration["record"]![0]!["fixture"]);
    }

    [Fact]
    public async Task What_Jev_said_is_shown_back_to_it()
    {
        // Arrange
        var jev = GivenJev.WithRecord(Said("Arsenal", "Chelsea", 0.95, 0.04, 0.01, 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.95, (double?)jev.Calibration["record"]![0]!["you_said"]!["home_win"]);
    }

    [Fact]
    public async Task What_actually_happened_is_shown_beside_it()
    {
        // Arrange
        var jev = GivenJev.WithRecord(Said("Arsenal", "Chelsea", 0.95, 0.04, 0.01, 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("2-1", (string?)jev.Calibration["record"]![0]!["actual_score"]);
    }

    [Fact]
    public async Task A_call_that_came_off_is_marked_right()
    {
        // Arrange
        var jev = GivenJev.WithRecord(Said("Arsenal", "Chelsea", 0.95, 0.04, 0.01, 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.True((bool?)jev.Calibration["record"]![0]!["you_were_right"]);
    }

    [Fact]
    public async Task A_call_that_missed_is_marked_wrong()
    {
        // Arrange
        // Said away win at 90%; the home side won.
        var jev = GivenJev.WithRecord(Said("Arsenal", "Chelsea", 0.05, 0.05, 0.90, 3, 0)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.False((bool?)jev.Calibration["record"]![0]!["you_were_right"]);
    }

    [Fact]
    public async Task The_tally_is_spelled_out_so_Jev_need_not_count()
    {
        // Arrange
        var jev = GivenJev.WithRecord(
            Said("Arsenal", "Chelsea", 0.95, 0.04, 0.01, 2, 1),
            Said("Everton", "Fulham", 0.05, 0.05, 0.90, 3, 0),
            Said("Leeds United", "Burnley", 0.10, 0.10, 0.80, 0, 2)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, (int?)jev.Calibration["called_correctly"]);
    }

    [Fact]
    public async Task A_first_gameweek_with_no_record_sends_nothing_to_judge()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(jev.Calibration["record"]);
    }

    [Fact]
    public async Task The_instructions_point_Jev_at_its_record()
    {
        // Arrange
        var jev = GivenJev.WithRecord(Said("Arsenal", "Chelsea", 0.95, 0.04, 0.01, 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Contains("calibration", (string?)jev.QuestionNamed("outcome")["instructions"]);
    }
}
