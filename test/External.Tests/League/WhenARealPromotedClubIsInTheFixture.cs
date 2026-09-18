namespace External.Tests.League;

using Domain.History;
using Domain.Model;
using Domain.Schedule;
using External.Tests.Builders;

/// <summary>
/// Hull City and Coventry City came up into 2026-27 and appear nowhere in the three completed
/// seasons. These pin the stand-in against the real data.
/// </summary>
public class WhenARealPromotedClubIsInTheFixture
{
    private static LeagueContextSource Source() =>
        new(GivenAFile.WithPriorSeasons().BuildPriorSeasons());

    private static Fixture Against(string home, string away) =>
        new("espn:test", 5, new DateTimeOffset(2026, 9, 19, 14, 0, 0, TimeSpan.Zero), home, away);

    [Fact]
    public async Task Every_club_that_came_up_is_identified()
    {
        // Arrange
        var seasons = GivenAFile.WithPriorSeasons().BuildPriorSeasons();

        // Act
        var promoted = PromotedSides.In(await seasons.GetAsync());

        // Assert
        Assert.Equal(
            ["Burnley", "Ipswich Town", "Leeds United", "Leicester City", "Southampton", "Sunderland"],
            promoted.Select(p => p.Club).OrderBy(c => c, StringComparer.Ordinal));
    }

    [Fact]
    public async Task A_promoted_home_club_borrows_the_reference_record()
    {
        // Act
        var context = await Source().ForAsync(Against("Coventry City", "Arsenal"));

        // Assert
        Assert.Equal(RecordBasis.PromotedSides, context.HomeClubAtHome!.Basis);
    }

    [Fact]
    public async Task Promoted_sides_won_a_quarter_of_their_home_matches()
    {
        // Act
        var context = await Source().ForAsync(Against("Coventry City", "Arsenal"));

        // Assert
        // 114 home matches across six promotions: W26 D28 L60.
        Assert.Equal((114, 26, 28, 60), (context.HomeClubAtHome!.Played, context.HomeClubAtHome.Won,
                                         context.HomeClubAtHome.Drawn, context.HomeClubAtHome.Lost));
    }

    [Fact]
    public async Task Promoted_sides_conceded_heavily_away_from_home()
    {
        // Act
        var context = await Source().ForAsync(Against("Arsenal", "Hull City"));

        // Assert
        Assert.Equal((110, 232), (context.AwayClubAwayFromHome!.GoalsFor,
                                  context.AwayClubAwayFromHome.GoalsAgainst));
    }

    [Fact]
    public async Task An_established_club_keeps_its_own_record()
    {
        // Act
        var context = await Source().ForAsync(Against("Arsenal", "Hull City"));

        // Assert
        Assert.Equal(RecordBasis.OwnRecord, context.HomeClubAtHome!.Basis);
    }

    [Fact]
    public async Task Two_clubs_that_have_never_met_in_the_league_report_no_meetings()
    {
        // Act
        var context = await Source().ForAsync(Against("Coventry City", "Hull City"));

        // Assert
        Assert.Null(context.PreviousMeetings);
    }

    [Fact]
    public async Task Every_club_in_the_current_season_now_has_a_record_to_send()
    {
        // Arrange
        var source = Source();
        var fixtures = await new GameweekSchedule(
            GivenAFile.WithFixtures().BuildFixtureSource()).GetGameweekAsync(5);

        // Act
        var contexts = await Task.WhenAll(fixtures.Select(f => source.ForAsync(f)));

        // Assert
        Assert.DoesNotContain(contexts, c => c.HomeClubAtHome is null || c.AwayClubAwayFromHome is null);
    }
}
