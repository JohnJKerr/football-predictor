namespace Domain.Tests.League;

using Domain.History;

/// <summary>
/// A club promoted into the league has no record of its own. Rather than send nothing, send
/// what clubs in that position have actually done: how promoted sides fared in the season
/// they came up.
/// </summary>
public class WhenProfilingAPromotedSide
{
    private static PriorResult Played(int season, string home, string away, int homeScore, int awayScore) =>
        new(new DateOnly(season, 9, 1), home, homeScore, away, awayScore);

    /// <summary>Establishes "Newcomers" as promoted into 2024 by having them absent in 2023.</summary>
    private static PriorResult[] Season(params PriorResult[] later) =>
        [Played(2023, "Arsenal", "Everton", 1, 0), .. later];

    [Fact]
    public void Its_home_matches_are_counted()
    {
        // Arrange
        var results = Season(
            Played(2024, "Newcomers", "Arsenal", 0, 2),
            Played(2024, "Newcomers", "Everton", 1, 1),
            Played(2024, "Arsenal", "Newcomers", 3, 0));

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Equal(2, profile!.AtHome.Played);
    }

    [Fact]
    public void Its_home_results_are_counted_from_its_own_point_of_view()
    {
        // Arrange
        var results = Season(
            Played(2024, "Newcomers", "Arsenal", 0, 2),
            Played(2024, "Newcomers", "Everton", 1, 1));

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Equal((0, 1, 1), (profile!.AtHome.Won, profile.AtHome.Drawn, profile.AtHome.Lost));
    }

    [Fact]
    public void Its_away_results_are_counted_from_its_own_point_of_view()
    {
        // Arrange
        var results = Season(
            Played(2024, "Arsenal", "Newcomers", 3, 0),
            Played(2024, "Everton", "Newcomers", 1, 2));

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Equal((1, 0, 1), (profile!.AwayFromHome.Won, profile.AwayFromHome.Drawn, profile.AwayFromHome.Lost));
    }

    [Fact]
    public void Its_goals_away_from_home_are_counted_from_its_own_point_of_view()
    {
        // Arrange
        var results = Season(Played(2024, "Arsenal", "Newcomers", 3, 1));

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Equal((1, 3), (profile!.AwayFromHome.GoalsFor, profile.AwayFromHome.GoalsAgainst));
    }

    [Fact]
    public void Several_promoted_clubs_are_pooled_into_one_reference()
    {
        // Arrange
        var results = Season(
            Played(2024, "Newcomers", "Arsenal", 0, 2),
            Played(2024, "Others", "Everton", 1, 1));

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Equal(2, profile!.AtHome.Played);
    }

    [Fact]
    public void Only_the_season_a_club_came_up_counts_towards_the_reference()
    {
        // Arrange
        // Newcomers stayed up, so their second season is an established club's season.
        var results = Season(
            Played(2024, "Newcomers", "Arsenal", 0, 2),
            Played(2025, "Newcomers", "Arsenal", 4, 0));

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Equal(1, profile!.AtHome.Played);
    }

    [Fact]
    public void A_league_with_no_identifiable_promotions_has_no_reference()
    {
        // Arrange
        var results = new[] { Played(2023, "Arsenal", "Everton", 1, 0) };

        // Act
        var profile = PromotedSideProfile.From(results);

        // Assert
        Assert.Null(profile);
    }
}
