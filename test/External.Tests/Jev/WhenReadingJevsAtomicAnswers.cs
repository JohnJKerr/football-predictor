namespace External.Tests.Jev;

using Domain.Model;
using External.Jev;
using External.Tests.Builders;

public class WhenReadingJevsAtomicAnswers
{
    [Theory]
    [InlineData(Outcome.HomeWin, 0.24)]
    [InlineData(Outcome.Draw, 0.31)]
    [InlineData(Outcome.AwayWin, 0.45)]
    public async Task Each_outcome_keeps_its_probability(Outcome outcome, double expected)
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(expected, forecast.Outcome.ProbabilityOf(outcome));
    }

    [Fact]
    public async Task Jevs_confidence_in_the_outcome_is_carried_back()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.58, forecast.Outcome.Confidence);
    }

    [Fact]
    public async Task The_chance_of_three_or_more_goals_is_read()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.62, forecast.OverTwoAndAHalfGoals);
    }

    [Fact]
    public async Task The_chance_of_both_clubs_scoring_is_read()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.71, forecast.BothTeamsToScore);
    }

    [Fact]
    public async Task A_missing_atomic_answer_does_not_lose_the_scoreline()
    {
        // Arrange
        // The scoreline is the answer we were originally after; a narrower question
        // going missing should not cost us it.
        var jev = GivenJev.Replying(JevReplies.WithOnlyAScoreline).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1.0, forecast.Scorelines.ByScoreline[new Scoreline(1, 0)]);
    }

    [Fact]
    public async Task A_missing_outcome_answer_leaves_nothing_to_go_on()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.WithOnlyAScoreline).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Empty(forecast.Outcome.ByOutcome);
    }

    [Fact]
    public async Task A_missing_scoreline_is_still_reported_as_a_Jev_failure()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.WithNoAnswer).Build();

        // Act
        var reading = async () => await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        await Assert.ThrowsAsync<JevException>(reading);
    }
}
