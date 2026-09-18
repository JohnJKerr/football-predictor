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
    public async Task The_most_likely_score_is_reported_for_each_fixture()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14, (2, 1, 0.31), (1, 1, 0.22)).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Equal(new ScoreResponse(2, 1, 0.31), Assert.Single(Ok(result).Fixtures).MostLikelyScore);
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
    public async Task A_fixture_Jev_could_not_call_is_reported_without_a_score()
    {
        // Arrange
        var controller = GivenAGameweek.With("Everton", "Ipswich Town", 14).Build();

        // Act
        var result = await controller.Get(5);

        // Assert
        Assert.Null(Assert.Single(Ok(result).Fixtures).MostLikelyScore);
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
