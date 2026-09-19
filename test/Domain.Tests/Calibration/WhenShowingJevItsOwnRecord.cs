namespace Domain.Tests.Calibration;

using Domain.Calibration;
using Domain.History;
using Domain.Model;
using Domain.Predicting;

/// <summary>
/// Jev is shown what it said about recent gameweeks and what actually happened, so it can
/// judge for itself how confident to be. Only gameweeks that have been played can appear.
/// </summary>
public class WhenShowingJevItsOwnRecord
{
    private sealed class Log(params LoggedPrediction[] predictions) : IPredictionLog
    {
        public Task RecordAsync(int gameweek, IReadOnlyList<FixturePrediction> p, CancellationToken c = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<LoggedPrediction>> ForGameweekAsync(int gameweek, CancellationToken c = default)
            => Task.FromResult<IReadOnlyList<LoggedPrediction>>(
                [.. predictions.Where(x => x.Gameweek == gameweek)]);
    }

    private sealed class Results(params CompletedMatch[] matches) : IMatchHistory
    {
        public Task<IReadOnlyList<CompletedMatch>> GetCompletedAsync(CancellationToken c = default)
            => Task.FromResult<IReadOnlyList<CompletedMatch>>(matches);
    }

    private static LoggedPrediction Said(
        int gameweek, string home, string away, double h, double d, double a) =>
        new(gameweek, home, away,
            new OutcomeProbabilities(
                new Dictionary<Outcome, double>
                {
                    [Outcome.HomeWin] = h, [Outcome.Draw] = d, [Outcome.AwayWin] = a,
                }, 0.5),
            new Prediction(2, 1, 0.3));

    private static CompletedMatch Happened(string home, string away, int homeGoals, int awayGoals) =>
        new(new DateTimeOffset(2026, 9, 1, 14, 0, 0, TimeSpan.Zero),
            new TeamPerformance(home, homeGoals, null, null),
            new TeamPerformance(away, awayGoals, null, null));

    private static CalibrationFeedback Given(
        LoggedPrediction[] logged, CompletedMatch[] played, int gameweeks = 3) =>
        new(new Log(logged), new Results(played), gameweeks);

    [Fact]
    public async Task A_prediction_is_paired_with_what_actually_happened()
    {
        // Arrange
        var feedback = Given(
            [Said(4, "Arsenal", "Chelsea", 0.95, 0.04, 0.01)],
            [Happened("Arsenal", "Chelsea", 2, 1)]);

        // Act
        var record = await feedback.BeforeAsync(5);

        // Assert
        Assert.Equal((2, 1), (Assert.Single(record).ActualHomeScore, record[0].ActualAwayScore));
    }

    [Fact]
    public async Task What_Jev_said_is_carried_alongside_the_result()
    {
        // Arrange
        var feedback = Given(
            [Said(4, "Arsenal", "Chelsea", 0.95, 0.04, 0.01)],
            [Happened("Arsenal", "Chelsea", 2, 1)]);

        // Act
        var record = await feedback.BeforeAsync(5);

        // Assert
        Assert.Equal(0.95, Assert.Single(record).Said.ProbabilityOf(Outcome.HomeWin));
    }

    [Fact]
    public async Task Only_the_gameweeks_before_the_one_being_predicted_are_shown()
    {
        // Arrange
        var feedback = Given(
            [Said(4, "Arsenal", "Chelsea", 0.9, 0.05, 0.05),
             Said(5, "Everton", "Fulham", 0.5, 0.3, 0.2)],
            [Happened("Arsenal", "Chelsea", 2, 1), Happened("Everton", "Fulham", 1, 1)]);

        // Act
        var record = await feedback.BeforeAsync(5);

        // Assert
        Assert.Equal([4], record.Select(r => r.Gameweek));
    }

    [Fact]
    public async Task No_more_than_the_configured_number_of_gameweeks_is_shown()
    {
        // Arrange
        var feedback = Given(
            [Said(2, "Arsenal", "Chelsea", 0.9, 0.05, 0.05),
             Said(3, "Everton", "Fulham", 0.5, 0.3, 0.2),
             Said(4, "Leeds United", "Burnley", 0.4, 0.3, 0.3)],
            [Happened("Arsenal", "Chelsea", 2, 1), Happened("Everton", "Fulham", 1, 1),
             Happened("Leeds United", "Burnley", 0, 2)],
            gameweeks: 2);

        // Act
        var record = await feedback.BeforeAsync(5);

        // Assert
        Assert.Equal([4, 3], record.Select(r => r.Gameweek));
    }

    [Fact]
    public async Task A_fixture_that_has_not_been_played_cannot_be_shown()
    {
        // Arrange
        // Predicted, but the result is not in yet.
        var feedback = Given([Said(4, "Arsenal", "Chelsea", 0.9, 0.05, 0.05)], []);

        // Act
        var record = await feedback.BeforeAsync(5);

        // Assert
        Assert.Empty(record);
    }

    [Fact]
    public async Task The_opening_gameweek_has_no_record_to_show()
    {
        // Arrange
        var feedback = Given([Said(1, "Arsenal", "Chelsea", 0.9, 0.05, 0.05)],
                             [Happened("Arsenal", "Chelsea", 2, 1)]);

        // Act
        var record = await feedback.BeforeAsync(1);

        // Assert
        Assert.Empty(record);
    }

    [Fact]
    public async Task Whether_the_call_was_right_follows_from_the_result()
    {
        // Arrange
        var feedback = Given(
            [Said(4, "Arsenal", "Chelsea", 0.05, 0.05, 0.90)],
            [Happened("Arsenal", "Chelsea", 2, 1)]);

        // Act
        var record = await feedback.BeforeAsync(5);

        // Assert
        Assert.False(Assert.Single(record).CalledItRight);
    }
}
