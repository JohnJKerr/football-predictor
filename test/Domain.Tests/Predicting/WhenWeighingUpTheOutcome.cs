namespace Domain.Tests.Predicting;

using Domain.Model;
using Domain.Predicting;

public class WhenWeighingUpTheOutcome
{
    private static OutcomeProbabilities Given(double home, double draw, double away) =>
        new(new Dictionary<Outcome, double>
        {
            [Outcome.HomeWin] = home,
            [Outcome.Draw] = draw,
            [Outcome.AwayWin] = away,
        },
        Confidence: 0.6);

    [Theory]
    [InlineData(0.55, 0.25, 0.20, Outcome.HomeWin)]
    [InlineData(0.20, 0.55, 0.25, Outcome.Draw)]
    [InlineData(0.20, 0.25, 0.55, Outcome.AwayWin)]
    public void The_likeliest_outcome_is_the_one_holding_most_probability(
        double home, double draw, double away, Outcome expected)
    {
        // Arrange
        var probabilities = Given(home, draw, away);

        // Act
        var likeliest = probabilities.MostLikely;

        // Assert
        Assert.Equal(expected, likeliest);
    }

    [Fact]
    public void A_draw_is_reported_when_a_home_and_away_win_are_equally_likely()
    {
        // Arrange
        // A genuine coin-toss between the two clubs is a draw-shaped match.
        var probabilities = Given(0.40, 0.20, 0.40);

        // Act
        var likeliest = probabilities.MostLikely;

        // Assert
        Assert.Equal(Outcome.Draw, likeliest);
    }

    [Fact]
    public void An_outcome_Jev_said_nothing_about_is_treated_as_impossible()
    {
        // Arrange
        var probabilities = new OutcomeProbabilities(
            new Dictionary<Outcome, double> { [Outcome.Draw] = 0.9 }, Confidence: 0.7);

        // Act
        var homeWin = probabilities.ProbabilityOf(Outcome.HomeWin);

        // Assert
        Assert.Equal(0, homeWin);
    }
}
