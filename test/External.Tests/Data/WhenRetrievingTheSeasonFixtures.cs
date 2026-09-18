namespace External.Tests.Data;

using External.Data;

public class WhenRetrievingTheSeasonFixtures
{
    private static JsonFileFixtureSource Source() => new(DataFiles.Fixtures);

    [Fact]
    public async Task Every_fixture_in_the_published_season_is_read()
    {
        var fixtures = await Source().GetAllAsync();

        // 20 clubs playing each other home and away.
        Assert.Equal(380, fixtures.Count);
    }

    [Fact]
    public async Task The_teams_and_kickoff_of_a_known_fixture_are_read()
    {
        var fixtures = await Source().GetAllAsync();

        var opener = fixtures.Single(f => f.Id == "espn:401879301");

        Assert.Equal("Arsenal", opener.HomeTeam);
        Assert.Equal("Coventry City", opener.AwayTeam);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero),
            opener.KickoffUtc);
    }
}
