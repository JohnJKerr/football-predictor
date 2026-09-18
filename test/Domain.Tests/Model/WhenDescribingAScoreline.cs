namespace Domain.Tests.Model;

using Domain.Model;

public class WhenDescribingAScoreline
{
    [Theory]
    [InlineData(1, 0, Outcome.HomeWin)]
    [InlineData(3, 1, Outcome.HomeWin)]
    [InlineData(0, 0, Outcome.Draw)]
    [InlineData(2, 2, Outcome.Draw)]
    [InlineData(0, 1, Outcome.AwayWin)]
    [InlineData(1, 4, Outcome.AwayWin)]
    public void The_outcome_follows_from_the_goals(int home, int away, Outcome expected)
    {
        // Arrange
        var scoreline = new Scoreline(home, away);

        // Act
        var outcome = scoreline.Outcome;

        // Assert
        Assert.Equal(expected, outcome);
    }
}
