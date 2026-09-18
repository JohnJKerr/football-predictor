namespace Domain.Tests.League;

using Domain.History;
using Domain.Tests.Builders;

/// <summary>
/// Hull City and Coventry City have not played in the league for three seasons. Rather than
/// leave a hole where a fixture is hardest to call, they stand in for the class they belong
/// to: clubs in the season they came up.
/// </summary>
public class WhenAClubHasNoRecordOfItsOwn
{
    private sealed class Seasons(params PriorResult[] results) : IPriorSeasons
    {
        public Task<IReadOnlyList<PriorResult>> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PriorResult>>(results);
    }

    private static PriorResult Played(int season, string home, string away, int homeScore, int awayScore) =>
        new(new DateOnly(season, 9, 1), home, homeScore, away, awayScore);

    /// <summary>
    /// A league where "Newcomers" came up in 2024 and were beaten home and away, and where
    /// neither club of the upcoming fixture has ever played.
    /// </summary>
    private static LeagueContextSource Given() => new(new Seasons(
        Played(2023, "Arsenal", "Everton", 1, 0),
        Played(2024, "Arsenal", "Everton", 1, 0),
        Played(2024, "Newcomers", "Arsenal", 0, 3),
        Played(2024, "Everton", "Newcomers", 2, 1)));

    [Fact]
    public async Task The_home_club_stands_in_for_promoted_sides_at_home()
    {
        // Act
        var context = await Given().ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(RecordBasis.PromotedSides, context.HomeClubAtHome!.Basis);
    }

    [Fact]
    public async Task The_home_club_is_given_what_promoted_sides_did_at_home()
    {
        // Act
        var context = await Given().ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal((1, 0, 0, 1), (context.HomeClubAtHome!.Played, context.HomeClubAtHome.Won,
                                    context.HomeClubAtHome.Drawn, context.HomeClubAtHome.Lost));
    }

    [Fact]
    public async Task The_substituted_record_still_names_the_club_it_stands_for()
    {
        // Act
        var context = await Given().ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("Nottingham Forest", context.HomeClubAtHome!.Club);
    }

    [Fact]
    public async Task The_away_club_is_given_what_promoted_sides_did_away()
    {
        // Act
        var context = await Given().ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal((1, 0, 0, 1), (context.AwayClubAwayFromHome!.Played, context.AwayClubAwayFromHome.Won,
                                    context.AwayClubAwayFromHome.Drawn, context.AwayClubAwayFromHome.Lost));
    }

    [Fact]
    public async Task A_club_with_its_own_record_keeps_it()
    {
        // Arrange
        var source = new LeagueContextSource(new Seasons(
            Played(2023, "Arsenal", "Everton", 1, 0),
            Played(2024, "Arsenal", "Newcomers", 2, 0),
            Played(2024, "Nottingham Forest", "Arsenal", 1, 1)));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(RecordBasis.OwnRecord, context.HomeClubAtHome!.Basis);
    }

    [Fact]
    public async Task Standing_in_for_promoted_sides_does_not_invent_previous_meetings()
    {
        // Act
        var context = await Given().ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(context.PreviousMeetings);
    }
}
