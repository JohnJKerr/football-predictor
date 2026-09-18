namespace Domain.Tests.League;

using Domain.History;
using Domain.Tests.Builders;

public class WhenBuildingTheLeagueContextForAFixture
{
    private sealed class Seasons(params PriorResult[] results) : IPriorSeasons
    {
        public Task<IReadOnlyList<PriorResult>> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PriorResult>>(results);
    }

    private static LeagueContextSource Given(params PriorResult[] results) => new(new Seasons(results));

    private const string Home = "Nottingham Forest";
    private const string Away = "Coventry City";

    [Fact]
    public async Task The_home_clubs_record_counts_only_its_home_matches()
    {
        // Arrange
        var source = Given(
            APriorResult.Of(Home, 2, 0, "Everton"),
            APriorResult.Of(Home, 1, 1, "Arsenal"),
            APriorResult.Of("Chelsea", 3, 0, Home));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, context.HomeClubAtHome!.Played);
    }

    [Fact]
    public async Task The_home_clubs_wins_at_home_are_counted()
    {
        // Arrange
        var source = Given(
            APriorResult.Of(Home, 2, 0, "Everton"),
            APriorResult.Of(Home, 1, 1, "Arsenal"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1, context.HomeClubAtHome!.Won);
    }

    [Fact]
    public async Task The_home_clubs_goals_at_home_are_counted()
    {
        // Arrange
        var source = Given(
            APriorResult.Of(Home, 2, 0, "Everton"),
            APriorResult.Of(Home, 1, 3, "Arsenal"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(3, context.HomeClubAtHome!.GoalsFor);
    }

    [Fact]
    public async Task The_away_clubs_record_counts_only_its_away_matches()
    {
        // Arrange
        var source = Given(
            APriorResult.Of("Everton", 0, 2, Away),
            APriorResult.Of(Away, 4, 0, "Everton"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1, context.AwayClubAwayFromHome!.Played);
    }

    [Fact]
    public async Task The_away_clubs_wins_away_from_home_are_counted()
    {
        // Arrange
        var source = Given(
            APriorResult.Of("Everton", 0, 2, Away),
            APriorResult.Of("Arsenal", 1, 1, Away));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1, context.AwayClubAwayFromHome!.Won);
    }

    [Fact]
    public async Task A_club_with_neither_a_record_nor_a_reference_class_reports_nothing()
    {
        // Arrange
        // One season only, so no promotions can be identified to fall back on.
        var source = Given(APriorResult.Of("Everton", 1, 0, "Arsenal"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(context.HomeClubAtHome);
    }

    [Fact]
    public async Task Previous_meetings_between_the_two_clubs_are_counted()
    {
        // Arrange
        var source = Given(
            APriorResult.Of(Home, 2, 0, Away),
            APriorResult.Of(Away, 1, 1, Home),
            APriorResult.Of(Home, 3, 0, "Everton"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, context.PreviousMeetings!.Played);
    }

    [Fact]
    public async Task A_meeting_is_recorded_from_the_home_clubs_point_of_view()
    {
        // Arrange
        // The upcoming home club won once at home and lost once away.
        var source = Given(
            APriorResult.Of(Home, 2, 0, Away),
            APriorResult.Of(Away, 3, 1, Home));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal((1, 0, 1), (context.PreviousMeetings!.Won, context.PreviousMeetings.Drawn,
                                 context.PreviousMeetings.Lost));
    }

    [Fact]
    public async Task Two_clubs_that_have_never_met_report_no_meetings()
    {
        // Arrange
        var source = Given(APriorResult.Of(Home, 2, 0, "Everton"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(context.PreviousMeetings);
    }

    [Fact]
    public async Task The_league_base_rates_come_from_every_season_not_just_these_clubs()
    {
        // Arrange
        var source = Given(
            APriorResult.Of("Everton", 1, 1, "Arsenal"),
            APriorResult.Of("Chelsea", 2, 0, "Fulham"),
            APriorResult.Of(Home, 1, 0, Away),
            APriorResult.Of("Leeds United", 0, 1, "Burnley"));

        // Act
        var context = await source.ForAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(4, context.BaseRates.Matches);
    }
}
