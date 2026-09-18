namespace External.Tests.Data;

using External.Tests.Builders;

public class WhenRetrievingTheSeasonFixtures
{
    [Fact]
    public async Task Every_fixture_in_the_published_season_is_read()
    {
        // Arrange
        var source = GivenAFile.WithFixtures().BuildFixtureSource();

        // Act
        var fixtures = await source.GetAllAsync();

        // Assert
        // 20 clubs playing each other home and away.
        Assert.Equal(380, fixtures.Count);
    }

    [Fact]
    public async Task The_home_club_of_a_known_fixture_is_read()
    {
        // Arrange
        var source = GivenAFile.WithFixtures().BuildFixtureSource();

        // Act
        var fixtures = await source.GetAllAsync();

        // Assert
        Assert.Equal("Arsenal", fixtures.Single(f => f.Id == "espn:401879301").HomeTeam);
    }

    [Fact]
    public async Task The_away_club_of_a_known_fixture_is_read()
    {
        // Arrange
        var source = GivenAFile.WithFixtures().BuildFixtureSource();

        // Act
        var fixtures = await source.GetAllAsync();

        // Assert
        Assert.Equal("Coventry City", fixtures.Single(f => f.Id == "espn:401879301").AwayTeam);
    }

    [Fact]
    public async Task The_kickoff_of_a_known_fixture_is_read()
    {
        // Arrange
        var source = GivenAFile.WithFixtures().BuildFixtureSource();

        // Act
        var fixtures = await source.GetAllAsync();

        // Assert
        Assert.Equal(
            new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero),
            fixtures.Single(f => f.Id == "espn:401879301").KickoffUtc);
    }
}
