namespace Domain.Tests.History;

using Domain.Tests.Builders;

public class WhenSelectingRelevantHistory
{
    private static readonly DateTimeOffset Kickoff = AFixture.Upcoming.KickoffUtc;

    [Fact]
    public async Task Only_matches_involving_one_of_the_two_clubs_are_kept()
    {
        // Arrange
        var history = GivenAHistory.Of(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedOn(2, "Arsenal", "Liverpool"),
            AMatch.PlayedOn(3, "Hull City", "Coventry City")).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([3, 1], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task A_club_is_found_whether_it_played_at_home_or_away()
    {
        // Arrange
        var history = GivenAHistory.Of(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedOn(2, "Everton", "Nottingham Forest")).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, relevant.Count);
    }

    [Fact]
    public async Task The_most_recent_form_comes_first()
    {
        // Arrange
        var history = GivenAHistory.Of(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedOn(20, "Coventry City", "Arsenal"),
            AMatch.PlayedOn(10, "Hull City", "Nottingham Forest")).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([20, 10, 1], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task A_previous_meeting_between_the_two_clubs_appears_once()
    {
        // Arrange
        var history = GivenAHistory.Of(AMatch.PlayedOn(1, "Coventry City", "Nottingham Forest")).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Single(relevant);
    }

    [Fact]
    public async Task No_more_than_the_configured_number_of_matches_per_club_is_kept()
    {
        // Arrange
        var played = Enumerable.Range(1, 6)
            .Select(d => AMatch.PlayedOn(d, "Nottingham Forest", $"Club {d}"))
            .ToArray();
        var history = GivenAHistory.Of(played).KeepingAtMostPerClub(4).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([6, 5, 4, 3], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task The_fixture_being_predicted_is_never_part_of_its_own_history()
    {
        // Arrange
        // The dataset holds completed matches, so a fixture already played appears in it.
        // Handing it back would let Jev read the answer straight off the state.
        var history = GivenAHistory.Of(
            AMatch.PlayedAt(Kickoff, "Nottingham Forest", "Coventry City", 2, 1)).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Empty(relevant);
    }

    [Fact]
    public async Task Matches_played_after_the_fixture_kicks_off_are_excluded()
    {
        // Arrange
        var history = GivenAHistory.Of(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedAt(Kickoff.AddDays(7), "Coventry City", "Arsenal", 3, 0)).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([1], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task Form_is_counted_from_the_matches_that_preceded_the_fixture()
    {
        // Arrange
        // Eight earlier matches, capped at four per club, must not be filled out with later ones.
        var played = Enumerable.Range(1, 8)
            .Select(d => AMatch.PlayedOn(d, "Nottingham Forest", $"Club {d}"))
            .Append(AMatch.PlayedAt(Kickoff.AddDays(3), "Nottingham Forest", "Club Z", 9, 0))
            .ToArray();
        var history = GivenAHistory.Of(played).KeepingAtMostPerClub(4).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([8, 7, 6, 5], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task A_club_with_no_matches_yet_simply_contributes_nothing()
    {
        // Arrange
        var history = GivenAHistory.Of(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        var relevant = await history.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Single(relevant);
    }
}
