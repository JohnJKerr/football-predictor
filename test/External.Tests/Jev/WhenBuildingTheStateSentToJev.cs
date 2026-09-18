namespace External.Tests.Jev;

using External.Jev;
using External.Tests.Builders;

public class WhenBuildingTheStateSentToJev
{
    /// <summary>Far more history than Jev will accept in one request.</summary>
    private static Domain.History.CompletedMatch[] CrowdedSeason() =>
        [.. Enumerable.Range(1, 400).Select(i => AMatch.PlayedOn(1 + (i % 28), $"Club {i}", "Coventry City"))];

    [Theory]
    [InlineData("home_team", "Nottingham Forest")]
    [InlineData("away_team", "Coventry City")]
    public async Task The_fixture_under_question_names_its_clubs(string field, string expected)
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(8, "Hull City", "Coventry City")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(expected, (string?)jev.Fixture[field]);
    }

    [Fact]
    public async Task The_fixture_under_question_names_its_gameweek()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(8, "Hull City", "Coventry City")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(5, (int?)jev.Fixture["gameweek"]);
    }

    [Fact]
    public async Task Only_the_history_judged_relevant_to_this_fixture_is_sent()
    {
        // Arrange
        var jev = GivenJev.WithHistory(
            AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 2, 0),
            AMatch.PlayedOn(8, "Hull City", "Coventry City", 1, 1)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        // Most recent first: the 8th precedes the 1st.
        Assert.Equal(
            ["Hull City", "Nottingham Forest"],
            jev.History.Select(m => (string?)m!["home"]!["team"]));
    }

    [Fact]
    public async Task The_goals_scored_are_carried_through()
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton", 2, 0)).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, (int?)jev.History[0]!["home"]!["goals"]);
    }

    [Theory]
    [InlineData("shots", 14.0)]
    [InlineData("on_target", 5.0)]
    [InlineData("off_target", 6.0)]
    [InlineData("blocked", 3.0)]
    [InlineData("xg", 1.62)]
    [InlineData("possession", 55.4)]
    public async Task The_shooting_and_possession_figures_are_carried_through(string field, double expected)
    {
        // Arrange
        var jev = GivenJev.WithHistory(AMatch.PlayedOn(1, "Nottingham Forest", "Everton")).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(expected, (double?)jev.History[0]!["home"]![field]);
    }

    [Fact]
    public async Task A_fixture_between_two_clubs_with_no_history_sends_no_history()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Empty(jev.History);
    }

    [Fact]
    public async Task A_fixture_between_two_clubs_with_no_history_is_still_asked_about()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.NotNull(jev.Question);
    }

    [Fact]
    public async Task Jevs_context_limit_is_respected()
    {
        // Arrange
        var jev = GivenJev.WithHistory(CrowdedSeason()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.InRange(jev.RequestBytes, 1, JevPredictor.MaxRequestBytes);
    }

    [Fact]
    public async Task History_still_reaches_Jev_after_trimming()
    {
        // Arrange
        var jev = GivenJev.WithHistory(CrowdedSeason()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.NotEmpty(jev.History);
    }

    [Fact]
    public async Task The_oldest_matches_are_the_ones_dropped()
    {
        // Arrange
        var jev = GivenJev.WithHistory(CrowdedSeason()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        // Nothing kept is older than something dropped.
        var oldestKept = jev.History.Min(m => DateTimeOffset.Parse((string)m!["kickoff"]!));
        Assert.True(jev.GivenHistory.Count(m => m.KickoffUtc > oldestKept) <= jev.History.Count);
    }

    [Fact]
    public async Task History_is_trimmed_when_it_does_not_fit()
    {
        // Arrange
        var jev = GivenJev.WithHistory(CrowdedSeason()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.True(jev.History.Count < jev.GivenHistory.Count);
    }
}
