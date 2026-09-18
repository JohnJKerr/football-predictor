namespace Domain.Tests.Schedule;

using Domain.Schedule;
using Domain.Tests.Builders;

public class WhenPlacingFixturesIntoGameweeks
{
    [Fact]
    public async Task Gameweek_one_is_the_first_ten_fixtures_by_kickoff()
    {
        // Arrange
        var schedule = new GameweekSchedule(GivenASeason.Of(30).Build());

        // Act
        var gameweek = await schedule.GetGameweekAsync(1);

        // Assert
        Assert.Equal(Enumerable.Range(0, 10).Select(i => $"match-{i}"), gameweek.Select(f => f.Id));
    }

    [Fact]
    public async Task Later_gameweeks_continue_in_blocks_of_ten()
    {
        // Arrange
        var schedule = new GameweekSchedule(GivenASeason.Of(30).Build());

        // Act
        var gameweek = await schedule.GetGameweekAsync(3);

        // Assert
        Assert.Equal(Enumerable.Range(20, 10).Select(i => $"match-{i}"), gameweek.Select(f => f.Id));
    }

    [Fact]
    public async Task Fixtures_are_stamped_with_the_gameweek_they_belong_to()
    {
        // Arrange
        var schedule = new GameweekSchedule(GivenASeason.Of(30).Build());

        // Act
        var gameweek = await schedule.GetGameweekAsync(2);

        // Assert
        Assert.Equal(Enumerable.Repeat(2, 10), gameweek.Select(f => f.Gameweek));
    }

    [Fact]
    public async Task Source_order_does_not_matter_only_kickoff_time_does()
    {
        // Arrange
        var schedule = new GameweekSchedule(GivenASeason.Of(20).InReverseOrder().Build());

        // Act
        var gameweek = await schedule.GetGameweekAsync(1);

        // Assert
        Assert.Equal(Enumerable.Range(0, 10).Select(i => $"match-{i}"), gameweek.Select(f => f.Id));
    }

    [Fact]
    public async Task Simultaneous_kickoffs_stay_in_a_stable_order()
    {
        // Arrange
        var season = GivenASeason.Of(20).AllKickingOffTogether();
        var first = await new GameweekSchedule(season.Build()).GetGameweekAsync(1);

        // Act
        var again = await new GameweekSchedule(season.InReverseOrder().Build()).GetGameweekAsync(1);

        // Assert
        Assert.Equal(first.Select(f => f.Id), again.Select(f => f.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task A_gameweek_outside_the_season_has_no_fixtures(int gameweek)
    {
        // Arrange
        var schedule = new GameweekSchedule(GivenASeason.Of(30).Build());

        // Act
        var result = await schedule.GetGameweekAsync(gameweek);

        // Assert
        Assert.Empty(result);
    }
}
