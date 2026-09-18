namespace Domain.Tests.League;

using Domain.History;
using Domain.Tests.Builders;

public class WhenMeasuringLeagueBaseRates
{
    private static LeagueBaseRates From(params PriorResult[] results) => LeagueBaseRates.From(results);

    [Fact]
    public void The_number_of_matches_behind_the_rates_is_reported()
    {
        // Arrange
        var results = APriorResult.Repeated(8, 1, 0).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(8, rates.Matches);
    }

    [Fact]
    public void The_share_of_home_wins_is_measured()
    {
        // Arrange
        var results = APriorResult.Repeated(3, 2, 0).Concat(APriorResult.Repeated(1, 0, 1)).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(0.75, rates.HomeWin);
    }

    [Fact]
    public void The_share_of_draws_is_measured()
    {
        // Arrange
        var results = APriorResult.Repeated(1, 1, 1).Concat(APriorResult.Repeated(3, 2, 0)).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(0.25, rates.Draw);
    }

    [Fact]
    public void The_share_of_away_wins_is_measured()
    {
        // Arrange
        var results = APriorResult.Repeated(1, 0, 2).Concat(APriorResult.Repeated(1, 1, 1)).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(0.5, rates.AwayWin);
    }

    [Fact]
    public void The_goals_scored_per_match_is_measured()
    {
        // Arrange
        var results = APriorResult.Repeated(2, 2, 1).Concat(APriorResult.Repeated(2, 0, 1)).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(2.0, rates.GoalsPerMatch);
    }

    [Fact]
    public void The_share_of_matches_passing_two_and_a_half_goals_is_measured()
    {
        // Arrange
        var results = APriorResult.Repeated(1, 2, 1).Concat(APriorResult.Repeated(3, 1, 0)).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(0.25, rates.OverTwoAndAHalfGoals);
    }

    [Fact]
    public void The_share_of_matches_where_both_clubs_scored_is_measured()
    {
        // Arrange
        var results = APriorResult.Repeated(1, 1, 1).Concat(APriorResult.Repeated(1, 3, 0)).ToArray();

        // Act
        var rates = From(results);

        // Assert
        Assert.Equal(0.5, rates.BothTeamsToScore);
    }

    [Fact]
    public void A_league_with_no_history_reports_no_matches()
    {
        // Act
        var rates = From();

        // Assert
        Assert.Equal(0, rates.Matches);
    }

    [Fact]
    public void A_league_with_no_history_does_not_divide_by_zero()
    {
        // Act
        var rates = From();

        // Assert
        Assert.Equal(0, rates.Draw);
    }
}
