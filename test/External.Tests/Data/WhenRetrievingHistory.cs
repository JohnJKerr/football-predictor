namespace External.Tests.Data;

using External.Data;

public class WhenRetrievingHistory
{
    private static JsonFileMatchHistory History() => new(DataFiles.Results);

    [Fact]
    public async Task Every_completed_match_of_the_season_so_far_is_read()
    {
        Assert.Equal(40, (await History().GetCompletedAsync()).Count);
    }

    [Fact]
    public async Task The_result_of_a_known_match_is_read()
    {
        var matches = await History().GetCompletedAsync();

        var opener = matches.Single(m =>
            m.Home.Name == "Arsenal" && m.Away.Name == "Coventry City");

        Assert.Equal(3, opener.Home.Goals);
        Assert.Equal(0, opener.Away.Goals);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero),
            opener.KickoffUtc);
    }

    [Fact]
    public async Task The_shooting_and_possession_figures_that_bear_on_a_scoreline_are_kept()
    {
        var matches = await History().GetCompletedAsync();

        var stats = matches
            .Single(m => m.Home.Name == "Arsenal" && m.Away.Name == "Coventry City")
            .Home.Stats;

        Assert.Equal(20, stats!.Shots);
        Assert.Equal(6, stats.ShotsOnTarget);
        Assert.Equal(8, stats.BlockedShots);
        Assert.Equal(1.8033, stats.ExpectedGoals);
        Assert.Equal(64.5, stats.PossessionPercent);
    }

    [Fact]
    public async Task A_club_with_no_league_position_before_kickoff_is_read_as_unknown()
    {
        var matches = await History().GetCompletedAsync();

        // Nobody has a position before the season opener.
        var opener = matches.Single(m => m.Home.Name == "Arsenal" && m.Away.Name == "Coventry City");

        Assert.Null(opener.Home.LeaguePositionBefore);
    }

    [Fact]
    public async Task The_file_is_read_once_and_reused()
    {
        var history = History();

        Assert.Same(await history.GetCompletedAsync(), await history.GetCompletedAsync());
    }
}
