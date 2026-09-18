namespace External.Tests.League;

using Domain.History;
using External.Tests.Builders;

/// <summary>
/// The rates that anchor Jev's answers, measured against the real three seasons. Backtesting
/// showed Jev putting 43% on away wins where the league runs 32%, so these are the numbers
/// that correct it — if they drift, so does the correction.
/// </summary>
public class WhenMeasuringTheRealLeaguesBaseRates
{
    private static async Task<LeagueBaseRates> Rates() =>
        LeagueBaseRates.From(await GivenAFile.WithPriorSeasons().BuildPriorSeasons().GetAsync());

    [Fact]
    public async Task Roughly_two_in_five_matches_are_won_at_home()
    {
        // Act
        var rates = await Rates();

        // Assert
        Assert.Equal(0.432, rates.HomeWin);
    }

    [Fact]
    public async Task Roughly_one_in_four_matches_is_drawn()
    {
        // Act
        var rates = await Rates();

        // Assert
        Assert.Equal(0.245, rates.Draw);
    }

    [Fact]
    public async Task Roughly_one_in_three_matches_is_won_away()
    {
        // Act
        var rates = await Rates();

        // Assert
        Assert.Equal(0.324, rates.AwayWin);
    }

    [Fact]
    public async Task Just_under_three_goals_are_scored_per_match()
    {
        // Act
        var rates = await Rates();

        // Assert
        Assert.Equal(2.988, rates.GoalsPerMatch);
    }

    [Fact]
    public async Task Nearly_three_in_five_matches_pass_two_and_a_half_goals()
    {
        // Act
        var rates = await Rates();

        // Assert
        Assert.Equal(0.588, rates.OverTwoAndAHalfGoals);
    }

    [Fact]
    public async Task Nearly_three_in_five_matches_see_both_clubs_score()
    {
        // Act
        var rates = await Rates();

        // Assert
        Assert.Equal(0.583, rates.BothTeamsToScore);
    }
}
