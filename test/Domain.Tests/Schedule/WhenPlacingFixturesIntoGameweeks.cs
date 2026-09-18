namespace Domain.Tests.Schedule;

using Domain.Schedule;

public class WhenPlacingFixturesIntoGameweeks
{
    /// <summary>
    /// Builds <paramref name="count"/> fixtures an hour apart, so kickoff order is unambiguous.
    /// Teams are numbered per fixture; grouping does not depend on who is playing.
    /// </summary>
    private static List<FixtureListing> Season(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new FixtureListing(
            Id: $"match-{i}",
            KickoffUtc: new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero).AddHours(i),
            HomeTeam: $"Home {i}",
            AwayTeam: $"Away {i}"))];

    private static GameweekSchedule ScheduleOf(IReadOnlyList<FixtureListing> fixtures)
        => new(new StubFixtureSource(fixtures));

    [Fact]
    public async Task Gameweek_one_is_the_first_ten_fixtures_by_kickoff()
    {
        var gameweek = await ScheduleOf(Season(30)).GetGameweekAsync(1);

        Assert.Equal(10, gameweek.Count);
        Assert.Equal("match-0", gameweek[0].Id);
        Assert.Equal("match-9", gameweek[9].Id);
    }

    [Fact]
    public async Task Later_gameweeks_continue_in_blocks_of_ten()
    {
        var gameweek = await ScheduleOf(Season(30)).GetGameweekAsync(3);

        Assert.Equal(10, gameweek.Count);
        Assert.Equal("match-20", gameweek[0].Id);
        Assert.Equal("match-29", gameweek[9].Id);
    }

    [Fact]
    public async Task Fixtures_are_stamped_with_the_gameweek_they_belong_to()
    {
        var gameweek = await ScheduleOf(Season(30)).GetGameweekAsync(2);

        Assert.All(gameweek, fixture => Assert.Equal(2, fixture.Gameweek));
    }

    [Fact]
    public async Task Source_order_does_not_matter_only_kickoff_time_does()
    {
        var shuffled = Season(20);
        shuffled.Reverse();

        var gameweek = await ScheduleOf(shuffled).GetGameweekAsync(1);

        Assert.Equal("match-0", gameweek[0].Id);
        Assert.Equal("match-9", gameweek[9].Id);
    }

    [Fact]
    public async Task Simultaneous_kickoffs_stay_in_a_stable_order()
    {
        var sameTime = new DateTimeOffset(2026, 9, 19, 14, 0, 0, TimeSpan.Zero);
        var fixtures = Season(20)
            .Select(f => f with { KickoffUtc = sameTime })
            .ToList();

        var first = await ScheduleOf(fixtures).GetGameweekAsync(1);
        var again = await ScheduleOf([.. Enumerable.Reverse(fixtures)]).GetGameweekAsync(1);

        Assert.Equal(first.Select(f => f.Id), again.Select(f => f.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task A_gameweek_outside_the_season_has_no_fixtures(int gameweek)
    {
        var result = await ScheduleOf(Season(30)).GetGameweekAsync(gameweek);

        Assert.Empty(result);
    }
}
