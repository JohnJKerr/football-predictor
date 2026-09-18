namespace Domain.Tests.League;

using Domain.History;

public class WhenIdentifyingPromotedSides
{
    private static PriorResult Played(int season, string home, string away, int homeScore = 1, int awayScore = 0) =>
        new(new DateOnly(season, 9, 1), home, homeScore, away, awayScore);

    [Fact]
    public void A_club_absent_the_season_before_has_just_come_up()
    {
        // Arrange
        var results = new[]
        {
            Played(2023, "Arsenal", "Everton"),
            Played(2024, "Arsenal", "Ipswich Town"),
        };

        // Act
        var promoted = PromotedSides.In(results);

        // Assert
        Assert.Equal([("Ipswich Town", 2024)], promoted.Select(p => (p.Club, p.Season)));
    }

    [Fact]
    public void A_club_present_throughout_has_not_come_up()
    {
        // Arrange
        var results = new[]
        {
            Played(2023, "Arsenal", "Everton"),
            Played(2024, "Arsenal", "Everton"),
        };

        // Act
        var promoted = PromotedSides.In(results);

        // Assert
        Assert.Empty(promoted);
    }

    [Fact]
    public void The_earliest_season_cannot_say_who_came_up()
    {
        // Arrange
        // Every club looks new when there is no season before it to compare against.
        var results = new[] { Played(2023, "Arsenal", "Everton") };

        // Act
        var promoted = PromotedSides.In(results);

        // Assert
        Assert.Empty(promoted);
    }

    [Fact]
    public void A_club_that_came_up_twice_is_counted_for_each_promotion()
    {
        // Arrange
        var results = new[]
        {
            Played(2023, "Arsenal", "Everton"),
            Played(2024, "Arsenal", "Luton Town"),
            Played(2025, "Arsenal", "Everton"),
            Played(2026, "Arsenal", "Luton Town"),
        };

        // Act
        var promoted = PromotedSides.In(results);

        // Assert
        Assert.Equal([2024, 2026], promoted.Where(p => p.Club == "Luton Town").Select(p => p.Season));
    }
}
