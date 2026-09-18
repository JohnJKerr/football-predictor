namespace External.Tests.Data;

using Domain.History;
using External.Tests.Builders;

public class WhenRetrievingHistory
{
    private static async Task<CompletedMatch> Opener()
    {
        var matches = await GivenAFile.WithResults().BuildMatchHistory().GetCompletedAsync();
        return matches.Single(m => m.Home.Name == "Arsenal" && m.Away.Name == "Coventry City");
    }

    [Fact]
    public async Task Every_completed_match_of_the_season_so_far_is_read()
    {
        // Arrange
        var history = GivenAFile.WithResults().BuildMatchHistory();

        // Act
        var matches = await history.GetCompletedAsync();

        // Assert
        Assert.Equal(40, matches.Count);
    }

    [Fact]
    public async Task The_goals_scored_by_the_home_club_are_read()
    {
        // Act
        var opener = await Opener();

        // Assert
        Assert.Equal(3, opener.Home.Goals);
    }

    [Fact]
    public async Task The_goals_scored_by_the_away_club_are_read()
    {
        // Act
        var opener = await Opener();

        // Assert
        Assert.Equal(0, opener.Away.Goals);
    }

    [Fact]
    public async Task The_kickoff_of_a_completed_match_is_read()
    {
        // Act
        var opener = await Opener();

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero), opener.KickoffUtc);
    }

    [Fact]
    public async Task The_shooting_and_possession_figures_that_bear_on_a_scoreline_are_kept()
    {
        // Act
        var opener = await Opener();

        // Assert
        Assert.Equal(new MatchStats(20, 6, 6, 8, 1.8033, 64.5), opener.Home.Stats);
    }

    [Fact]
    public async Task A_club_with_no_league_position_before_kickoff_is_read_as_unknown()
    {
        // Act
        // Nobody has a position before the season opener.
        var opener = await Opener();

        // Assert
        Assert.Null(opener.Home.LeaguePositionBefore);
    }

    [Fact]
    public async Task The_file_is_read_once_and_reused()
    {
        // Arrange
        var history = GivenAFile.WithResults().BuildMatchHistory();
        var first = await history.GetCompletedAsync();

        // Act
        var second = await history.GetCompletedAsync();

        // Assert
        Assert.Same(first, second);
    }
}
