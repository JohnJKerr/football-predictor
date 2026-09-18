namespace Api.Tests.Controllers;

using Api.Models;
using Api.Tests.Builders;
using Microsoft.AspNetCore.Mvc;

public class WhenRequestingAGameweek
{
    private static GameweekResponse Ok(ActionResult<GameweekResponse> result) =>
        Assert.IsType<GameweekResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);

    [Fact]
    public async Task The_home_club_of_each_fixture_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31), (1, 1, 0.22)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal("Everton", Assert.Single(Ok(result).Fixtures).HomeTeam);
    }

    [Fact]
    public async Task The_away_club_of_each_fixture_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal("Ipswich Town", Assert.Single(Ok(result).Fixtures).AwayTeam);
    }

    [Fact]
    public async Task The_likeliest_score_leads_the_list_for_each_fixture()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31), (1, 1, 0.22)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(new ScoreResponse(2, 1, 0.31), Assert.Single(Ok(result).Fixtures).Scorelines[0]);
    }

    [Fact]
    public async Task The_gameweek_that_was_asked_for_is_echoed_back()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (1, 0, 0.3)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(5, Ok(result).Gameweek);
    }

    [Fact]
    public async Task The_gameweek_that_was_asked_for_is_the_one_predicted()
    {
        // Arrange
        var predictor = GivenAGameweek.With("Everton", "Ipswich Town", 14, (1, 0, 0.3)).BuildPredictor();

        // Act
        await new Api.Controllers.GameweeksController(predictor).Get(5);

        // Assert
        Assert.Equal(5, predictor.Asked);
    }

    [Fact]
    public async Task Fixtures_keep_the_order_the_domain_gave_them()
    {
        // Arrange
        var controller = GivenAGameweek
            .With("Tottenham Hotspur", "Aston Villa", 11, (1, 1, 0.2))
            .And("Everton", "Ipswich Town", 14, (2, 0, 0.3))
            .And("Nottingham Forest", "Coventry City", 16, (0, 1, 0.4))
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(
            ["Tottenham Hotspur", "Everton", "Nottingham Forest"],
            Ok(result).Fixtures.Select(f => f.HomeTeam));
    }

    [Fact]
    public async Task The_likeliest_outcome_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31))
            .Forecasting(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58, over: 0.62, bothScore: 0.71)
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal("AwayWin", Assert.Single(Ok(result).Fixtures).Outcome!.Result);
    }

    [Fact]
    public async Task The_probability_of_each_outcome_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31))
            .Forecasting(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58, over: 0.62, bothScore: 0.71)
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(
            (0.24, 0.31, 0.45),
            Assert.Single(Ok(result).Fixtures).Outcome switch
            {
                { } o => (o.HomeWin, o.Draw, o.AwayWin),
                null => (0d, 0d, 0d),
            });
    }

    [Fact]
    public async Task Jevs_confidence_in_the_outcome_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31))
            .Forecasting(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58, over: 0.62, bothScore: 0.71)
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(0.58, Assert.Single(Ok(result).Fixtures).Outcome!.Confidence);
    }

    [Fact]
    public async Task The_chance_of_three_or_more_goals_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31))
            .Forecasting(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58, over: 0.62, bothScore: 0.71)
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(0.62, Assert.Single(Ok(result).Fixtures).OverTwoAndAHalfGoals);
    }

    [Fact]
    public async Task The_chance_of_both_clubs_scoring_is_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31))
            .Forecasting(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58, over: 0.62, bothScore: 0.71)
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(0.71, Assert.Single(Ok(result).Fixtures).BothTeamsToScore);
    }

    [Fact]
    public async Task A_fixture_Jev_gave_no_outcome_for_is_reported_without_one()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Null(Assert.Single(Ok(result).Fixtures).Outcome);
    }

    [Fact]
    public async Task Several_candidate_scorelines_are_reported_most_likely_first()
    {
        // Arrange
        // An exact scoreline is never confident, so showing one number alone overstates it.
        var controller = GivenAGameweek
            .With("Everton", "Ipswich Town", 14, (2, 1, 0.25), (1, 1, 0.19), (2, 0, 0.15))
            .Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(
            [(2, 1), (1, 1), (2, 0)],
            Assert.Single(Ok(result).Fixtures).Scorelines.Select(s => (s.Home, s.Away)));
    }

    [Fact]
    public async Task Only_the_number_of_scorelines_asked_for_are_reported()
    {
        // Arrange
        var controller = GivenAGameweek
            .With("Everton", "Ipswich Town", 14, (2, 1, 0.25), (1, 1, 0.19), (2, 0, 0.15))
            .Build();

        // Act
        var result = await controller.Get(5, scorelines: 2);

        // Assert
        Assert.Equal(2, Assert.Single(Ok(result).Fixtures).Scorelines.Count);
    }

    [Fact]
    public async Task The_probability_left_on_scorelines_not_shown_is_reported()
    {
        // Arrange
        // What is left over is the point: it says how much the shown scorelines leave out.
        var controller = GivenAGameweek
            .With("Everton", "Ipswich Town", 14, (2, 1, 0.25), (1, 1, 0.19), (2, 0, 0.15))
            .Build();

        // Act
        var result = await controller.Get(5, scorelines: 2);

        // Assert
        Assert.Equal(0.56, Assert.Single(Ok(result).Fixtures).OtherScorelines, 3);
    }

    [Fact]
    public async Task A_fixture_whose_scorelines_are_all_shown_still_reports_what_is_left()
    {
        // Arrange
        var controller = GivenAGameweek
            .With("Everton", "Ipswich Town", 14, (2, 1, 0.25), (1, 1, 0.19)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(0.56, Assert.Single(Ok(result).Fixtures).OtherScorelines, 3);
    }

    [Fact]
    public async Task A_gameweek_with_no_fixtures_is_not_found()
    {
        // Arrange
        var controller = GivenAGameweek.WithNoFixtures().Build();

        // Act
        var result = await controller.Get(99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task A_fixture_Jev_could_not_call_is_reported_without_any_scoreline()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Empty(Assert.Single(Ok(result).Fixtures).Scorelines);
    }

    [Fact]
    public async Task A_fixture_Jev_could_not_call_is_still_reported()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal("Everton", Assert.Single(Ok(result).Fixtures).HomeTeam);
    }
}
