namespace External.Tests.Jev;

using External.Tests.Builders;

/// <summary>
/// Jev cannot know how the league behaves in aggregate. Backtesting had it putting 43% on
/// away wins where the league runs 32%, so the base rates travel with every question.
/// </summary>
public class WhenSendingTheLeagueContextToJev
{
    [Theory]
    [InlineData("home_win", 0.432)]
    [InlineData("draw", 0.245)]
    [InlineData("away_win", 0.324)]
    [InlineData("goals_per_match", 2.988)]
    [InlineData("over_two_and_a_half_goals", 0.588)]
    [InlineData("both_teams_to_score", 0.583)]
    public async Task The_league_base_rates_are_sent(string field, double expected)
    {
        // Arrange
        var jev = GivenJev.WithLeague(ALeague.Of()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(expected, (double?)jev.League["base_rates"]![field]);
    }

    [Fact]
    public async Task The_number_of_matches_behind_the_rates_is_sent()
    {
        // Arrange
        var jev = GivenJev.WithLeague(ALeague.Of()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(1140, (int?)jev.League["base_rates"]!["matches"]);
    }

    [Fact]
    public async Task The_home_clubs_record_at_home_is_sent()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(homeAtHome: ALeague.Record("Nottingham Forest"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(18, (int?)jev.League["home_club_at_home"]!["won"]);
    }

    [Fact]
    public async Task The_away_clubs_record_away_from_home_is_sent()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(awayFromHome: ALeague.Record("Coventry City"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(57, (int?)jev.League["away_club_away_from_home"]!["played"]);
    }

    [Fact]
    public async Task Previous_meetings_between_the_clubs_are_sent()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(meetings: new Domain.History.HeadToHead(6, 3, 1, 2))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(3, (int?)jev.League["previous_meetings"]!["won"]);
    }

    [Fact]
    public async Task A_newly_promoted_club_sends_no_record_rather_than_an_invented_one()
    {
        // Arrange
        // Coventry City have not played in the league for three seasons.
        var jev = GivenJev.WithLeague(ALeague.Of(homeAtHome: ALeague.Record("Nottingham Forest"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(jev.League["away_club_away_from_home"]);
    }

    [Fact]
    public async Task Two_clubs_that_have_never_met_send_no_meetings()
    {
        // Arrange
        var jev = GivenJev.WithLeague(ALeague.Of()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(jev.League["previous_meetings"]);
    }

    [Fact]
    public async Task The_instructions_point_Jev_at_the_base_rates()
    {
        // Arrange
        var jev = GivenJev.WithLeague(ALeague.Of()).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Contains("base_rates", (string?)jev.QuestionNamed("outcome")["instructions"]);
    }
}
