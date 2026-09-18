namespace External.Tests.Data;

using Domain.History;
using Domain.Schedule;
using External.Data;

/// <summary>
/// Backtesting runs the predictor over rounds the results dataset already contains. These
/// guard, against the real season, that a fixture is never shown its own result — the
/// failure that makes a backtest report near-perfect accuracy.
/// </summary>
public class WhenPredictingAFixtureAlreadyPlayed
{
    private static RelevantHistory History() => new(new JsonFileMatchHistory(DataFiles.Results));

    private static GameweekSchedule Schedule() => new(new JsonFileFixtureSource(DataFiles.Fixtures));

    [Fact]
    public async Task No_fixture_is_given_its_own_result()
    {
        var history = History();

        // Gameweeks 1-4 are complete, so every one of these fixtures is in the dataset.
        for (var gameweek = 1; gameweek <= 4; gameweek++)
        {
            foreach (var fixture in await Schedule().GetGameweekAsync(gameweek))
            {
                var relevant = await history.ForAsync(fixture);

                Assert.DoesNotContain(relevant, m =>
                    m.Involves(fixture.HomeTeam) && m.Involves(fixture.AwayTeam) &&
                    m.KickoffUtc == fixture.KickoffUtc);
            }
        }
    }

    [Fact]
    public async Task Nothing_played_after_kickoff_reaches_the_prediction()
    {
        var history = History();

        for (var gameweek = 1; gameweek <= 4; gameweek++)
        {
            foreach (var fixture in await Schedule().GetGameweekAsync(gameweek))
            {
                var relevant = await history.ForAsync(fixture);

                Assert.All(relevant, m =>
                    Assert.True(
                        m.KickoffUtc < fixture.KickoffUtc,
                        $"GW{gameweek} {fixture.HomeTeam} v {fixture.AwayTeam} was given a " +
                        $"match kicking off at {m.KickoffUtc:u}, at or after its own {fixture.KickoffUtc:u}."));
            }
        }
    }

    [Fact]
    public async Task The_opening_round_has_no_form_to_go_on()
    {
        var history = History();

        foreach (var fixture in await Schedule().GetGameweekAsync(1))
        {
            Assert.Empty(await history.ForAsync(fixture));
        }
    }

    [Fact]
    public async Task By_the_fourth_round_both_clubs_have_form_behind_them()
    {
        var history = History();

        foreach (var fixture in await Schedule().GetGameweekAsync(4))
        {
            var relevant = await history.ForAsync(fixture);

            Assert.Contains(relevant, m => m.Involves(fixture.HomeTeam));
            Assert.Contains(relevant, m => m.Involves(fixture.AwayTeam));
        }
    }
}
