namespace Domain.Tests.History;

using Domain.History;
using Domain.Tests.Builders;

public class WhenSummarisingRecentForm
{
    [Fact]
    public void Only_the_club_s_own_matches_are_counted()
    {
        // Arrange
        var window = new[]
        {
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedOn(2, "Arsenal", "Liverpool"),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(1, summary!.Matches);
    }

    [Fact]
    public void A_club_is_counted_whether_it_played_at_home_or_away()
    {
        // Arrange
        var window = new[]
        {
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedOn(2, "Everton", "Nottingham Forest"),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(2, summary!.Matches);
    }

    [Fact]
    public void Goals_scored_are_averaged_over_the_window()
    {
        // Arrange
        var window = new[]
        {
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 3, 0),
            AMatch.PlayedOn(2, "Everton", "Nottingham Forest", 1, 2),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(2.5, summary!.GoalsForPerMatch);
    }

    [Fact]
    public void Goals_conceded_are_taken_from_the_other_side_of_each_match()
    {
        // Arrange
        var window = new[]
        {
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 3, 0),
            AMatch.PlayedOn(2, "Everton", "Nottingham Forest", 1, 2),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(0.5, summary!.GoalsAgainstPerMatch);
    }

    [Fact]
    public void Expected_goals_are_averaged_the_same_way()
    {
        // Arrange
        var window = new[] { AMatch.PlayedOnWithXg(1, "Nottingham Forest", "Everton", 2.0, 1.0) };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(2.0, summary!.ExpectedGoalsForPerMatch);
    }

    [Fact]
    public void Expected_goals_conceded_come_from_the_opponent()
    {
        // Arrange
        var window = new[] { AMatch.PlayedOnWithXg(1, "Nottingham Forest", "Everton", 2.0, 1.0) };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(1.0, summary!.ExpectedGoalsAgainstPerMatch);
    }

    [Fact]
    public void Expected_goals_are_averaged_only_over_the_matches_that_carried_them()
    {
        // Arrange
        // One match with xG, one without: the average is the first alone, not halved by the second.
        var window = new[]
        {
            AMatch.PlayedOnWithXg(1, "Nottingham Forest", "Everton", 2.0, 1.0),
            AMatch.PlayedOn(2, "Nottingham Forest", "Arsenal"),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(2.0, summary!.ExpectedGoalsForPerMatch);
    }

    [Fact]
    public void The_number_of_matches_behind_the_expected_goals_is_reported()
    {
        // Arrange
        var window = new[]
        {
            AMatch.PlayedOnWithXg(1, "Nottingham Forest", "Everton", 2.0, 1.0),
            AMatch.PlayedOn(2, "Nottingham Forest", "Arsenal"),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(1, summary!.MatchesWithXg);
    }

    [Fact]
    public void A_window_with_no_expected_goals_at_all_reports_none()
    {
        // Arrange
        var window = new[] { AMatch.PlayedOn(1, "Nottingham Forest", "Everton") };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Null(summary!.ExpectedGoalsForPerMatch);
    }

    [Fact]
    public void A_club_with_no_matches_in_the_window_has_no_summary()
    {
        // Arrange
        var window = new[] { AMatch.PlayedOn(1, "Arsenal", "Liverpool") };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Null(summary);
    }

    [Fact]
    public void The_club_is_named_as_it_was_asked_for()
    {
        // Arrange
        var window = new[] { AMatch.PlayedOn(1, "Nottingham Forest", "Everton") };

        // Act
        var summary = FormSummary.For(window, "nottingham forest");

        // Assert
        Assert.Equal("nottingham forest", summary!.Club);
    }

    [Fact]
    public void Rates_are_rounded_to_the_places_worth_sending()
    {
        // Arrange
        var window = new[]
        {
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 1, 0),
            AMatch.PlayedOn(2, "Nottingham Forest", "Arsenal", 1, 0),
            AMatch.PlayedOn(3, "Nottingham Forest", "Hull City", 0, 0),
        };

        // Act
        var summary = FormSummary.For(window, "Nottingham Forest");

        // Assert
        Assert.Equal(0.667, summary!.GoalsForPerMatch);
    }
}
