namespace External.Tests.Data;

using Domain.History;
using Domain.Model;
using Domain.Schedule;
using External.Tests.Builders;

/// <summary>
/// Backtesting runs the predictor over rounds the results dataset already contains. These
/// guard, against the real season, that a fixture is never shown its own result — the
/// failure that makes a backtest report near-perfect accuracy.
/// </summary>
public class WhenPredictingAFixtureAlreadyPlayed
{
    private static RelevantHistory History() =>
        new(GivenAFile.WithResults().BuildMatchHistory());

    private static GameweekSchedule Schedule() =>
        new(GivenAFile.WithFixtures().BuildFixtureSource());

    /// <summary>Gameweeks 1-4 are complete, so every one of these fixtures is in the dataset.</summary>
    private static async Task<List<(Fixture Fixture, IReadOnlyList<CompletedMatch> Relevant)>> PlayedRounds()
    {
        var history = History();
        var rows = new List<(Fixture, IReadOnlyList<CompletedMatch>)>();

        for (var gameweek = 1; gameweek <= 4; gameweek++)
        {
            foreach (var fixture in await Schedule().GetGameweekAsync(gameweek))
            {
                rows.Add((fixture, await history.ForAsync(fixture)));
            }
        }

        return rows;
    }

    [Fact]
    public async Task No_fixture_is_given_its_own_result()
    {
        // Act
        var rounds = await PlayedRounds();

        // Assert
        Assert.DoesNotContain(rounds, row => row.Relevant.Any(m =>
            m.Involves(row.Fixture.HomeTeam) &&
            m.Involves(row.Fixture.AwayTeam) &&
            m.KickoffUtc == row.Fixture.KickoffUtc));
    }

    [Fact]
    public async Task Nothing_played_after_kickoff_reaches_the_prediction()
    {
        // Act
        var rounds = await PlayedRounds();

        // Assert
        Assert.DoesNotContain(rounds, row =>
            row.Relevant.Any(m => m.KickoffUtc >= row.Fixture.KickoffUtc));
    }

    [Fact]
    public async Task The_opening_round_has_no_form_to_go_on()
    {
        // Arrange
        var history = History();
        var opening = await Schedule().GetGameweekAsync(1);

        // Act
        var relevant = await Task.WhenAll(opening.Select(f => history.ForAsync(f)));

        // Assert
        Assert.All(relevant, Assert.Empty);
    }

    [Fact]
    public async Task By_the_fourth_round_both_clubs_have_form_behind_them()
    {
        // Arrange
        var history = History();
        var fourth = await Schedule().GetGameweekAsync(4);

        // Act
        var rows = await Task.WhenAll(fourth.Select(async f => (Fixture: f, Relevant: await history.ForAsync(f))));

        // Assert
        Assert.All(rows, row => Assert.Equal(
            [true, true],
            new[]
            {
                row.Relevant.Any(m => m.Involves(row.Fixture.HomeTeam)),
                row.Relevant.Any(m => m.Involves(row.Fixture.AwayTeam)),
            }));
    }
}
