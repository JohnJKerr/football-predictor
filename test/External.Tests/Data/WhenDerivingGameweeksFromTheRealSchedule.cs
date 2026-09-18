namespace External.Tests.Data;

using Domain.Schedule;
using External.Tests.Builders;

/// <summary>
/// The published dataset carries no matchweek field, so gameweeks are derived as blocks of
/// ten in kickoff order. These guard that derivation against the real season, and are what
/// would catch a fixture being moved across a block boundary.
/// </summary>
public class WhenDerivingGameweeksFromTheRealSchedule
{
    private static GameweekSchedule Schedule() =>
        new(GivenAFile.WithFixtures().BuildFixtureSource());

    [Fact]
    public async Task Every_gameweek_holds_ten_fixtures()
    {
        // Arrange
        var schedule = Schedule();

        // Act
        var counts = await Task.WhenAll(
            Enumerable.Range(1, 38).Select(async gw => (await schedule.GetGameweekAsync(gw)).Count));

        // Assert
        Assert.Equal(Enumerable.Repeat(10, 38), counts);
    }

    [Fact]
    public async Task Every_club_plays_exactly_once_in_each_gameweek()
    {
        // Arrange
        var schedule = Schedule();

        // Act
        var distinctClubs = await Task.WhenAll(
            Enumerable.Range(1, 38).Select(async gw =>
                (await schedule.GetGameweekAsync(gw))
                    .SelectMany(f => new[] { f.HomeTeam, f.AwayTeam })
                    .Distinct()
                    .Count()));

        // Assert
        Assert.Equal(Enumerable.Repeat(20, 38), distinctClubs);
    }

    [Fact]
    public async Task The_season_runs_out_after_thirty_eight_gameweeks()
    {
        // Arrange
        var schedule = Schedule();

        // Act
        var gameweek = await schedule.GetGameweekAsync(39);

        // Assert
        Assert.Empty(gameweek);
    }

    [Fact]
    public async Task Gameweek_five_holds_the_round_being_played_this_week()
    {
        // Arrange
        var schedule = Schedule();

        // Act
        var gameweek = await schedule.GetGameweekAsync(5);

        // Assert
        Assert.Contains(gameweek, f =>
            f.HomeTeam == "Nottingham Forest" && f.AwayTeam == "Coventry City");
    }

    [Fact]
    public async Task Gameweek_five_spans_its_own_weekend()
    {
        // Arrange
        var schedule = Schedule();

        // Act
        var gameweek = await schedule.GetGameweekAsync(5);

        // Assert
        // The round runs across 18-20 September 2026.
        Assert.All(gameweek, f => Assert.InRange(
            f.KickoffUtc,
            new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 20, 23, 59, 59, TimeSpan.Zero)));
    }
}
