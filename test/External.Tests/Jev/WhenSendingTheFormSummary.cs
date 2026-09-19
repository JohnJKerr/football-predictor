namespace External.Tests.Jev;

using External.Jev;
using External.Tests.Builders;

public class WhenSendingTheFormSummary
{
    [Fact]
    public async Task The_club_at_home_is_summarised()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("Nottingham Forest", (string?)jev.Form["home"]!["club"]);
    }

    [Fact]
    public async Task The_club_away_from_home_is_summarised()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Hull City", "Coventry City")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("Coventry City", (string?)jev.Form["away"]!["club"]);
    }

    [Fact]
    public async Task The_number_of_matches_behind_the_summary_is_stated()
    {
        // Arrange
        var jev = GivenJev.WithHistory(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton"),
            AMatch.PlayedOn(2, "Arsenal", "Nottingham Forest")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, (int?)jev.Form["home"]!["matches"]);
    }

    [Fact]
    public async Task Goals_scored_per_match_are_stated()
    {
        // Arrange
        // Two goals at home, then one away: an average of one and a half.
        var jev = GivenJev.WithHistory(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 2, 1),
            AMatch.PlayedOn(2, "Arsenal", "Nottingham Forest", 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1.5, (double?)jev.Form["home"]!["goals_for_per_match"]);
    }

    [Fact]
    public async Task Goals_conceded_per_match_are_stated()
    {
        // Arrange
        var jev = GivenJev.WithHistory(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 2, 1),
            AMatch.PlayedOn(2, "Arsenal", "Nottingham Forest", 2, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1.5, (double?)jev.Form["home"]!["goals_against_per_match"]);
    }

    [Fact]
    public async Task Expected_goals_per_match_are_stated()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1.62, (double?)jev.Form["home"]!["xg_for_per_match"]);
    }

    [Fact]
    public async Task Expected_goals_conceded_per_match_are_stated()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.91, (double?)jev.Form["home"]!["xg_against_per_match"]);
    }

    [Fact]
    public async Task How_many_matches_carried_expected_goals_is_stated()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1, (int?)jev.Form["home"]!["matches_with_xg"]);
    }

    [Fact]
    public async Task A_club_with_no_matches_in_the_window_is_left_out()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(jev.Form["away"]);
    }

    [Fact]
    public async Task Nothing_is_summarised_when_the_block_is_withheld()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton"))
            .AndState(StateSettings.Default)
            .Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Empty(jev.Form.AsObject());
    }

    [Fact]
    public async Task The_per_match_history_is_unaffected_by_the_summary()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Single(jev.History);
    }

    [Fact]
    public async Task The_summary_describes_the_matches_that_survived_trimming()
    {
        // Arrange
        // Far more history than fits, so the payload budget sheds some of it.
        var crowded = Enumerable.Range(1, 400)
            .Select(i => AMatch.PlayedOn(1 + (i % 28), "Nottingham Forest", $"Club {i}"))
            .ToArray();
        var jev = GivenJev.WithHistory(crowded).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(jev.History.Count, (int?)jev.Form["home"]!["matches"]);
    }
}
