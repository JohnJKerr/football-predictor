namespace External.Tests.Data;

using Domain.Schedule;
using External.Data;

/// <summary>
/// The published dataset carries no matchweek field, so gameweeks are derived as blocks of
/// ten in kickoff order. These guard that derivation against the real season, and are what
/// would catch a fixture being moved across a block boundary.
/// </summary>
public class WhenDerivingGameweeksFromTheRealSchedule
{
    private static GameweekSchedule Schedule() => new(new JsonFileFixtureSource(DataFiles.Fixtures));

    [Fact]
    public async Task Every_club_plays_exactly_once_in_each_of_the_thirty_eight_gameweeks()
    {
        var schedule = Schedule();

        for (var gameweek = 1; gameweek <= 38; gameweek++)
        {
            var fixtures = await schedule.GetGameweekAsync(gameweek);

            var clubs = fixtures
                .SelectMany(f => new[] { f.HomeTeam, f.AwayTeam })
                .ToList();

            Assert.Equal(10, fixtures.Count);
            Assert.Equal(20, clubs.Distinct().Count());
        }
    }

    [Fact]
    public async Task The_season_runs_out_after_thirty_eight_gameweeks()
    {
        Assert.Empty(await Schedule().GetGameweekAsync(39));
    }

    [Fact]
    public async Task Gameweek_five_holds_the_round_being_played_this_week()
    {
        var fixtures = await Schedule().GetGameweekAsync(5);

        Assert.Contains(fixtures, f =>
            f.HomeTeam == "Nottingham Forest" && f.AwayTeam == "Coventry City");

        // The round spans the weekend of 18-20 September 2026.
        Assert.All(fixtures, f =>
        {
            Assert.InRange(
                f.KickoffUtc,
                new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 20, 23, 59, 59, TimeSpan.Zero));
        });
    }
}
