namespace Api.Tests.Controllers;

using Api.Controllers;
using Api.Models;
using Domain.Model;
using Domain.Predicting;
using Microsoft.AspNetCore.Mvc;

public class WhenRequestingAGameweek
{
    private static Fixture FixtureAt(int hour, string home, string away) => new(
        Id: $"espn:{hour}",
        Gameweek: 5,
        KickoffUtc: new DateTimeOffset(2026, 9, 19, hour, 0, 0, TimeSpan.Zero),
        HomeTeam: home,
        AwayTeam: away);

    private static FixturePrediction Predicted(
        Fixture fixture, params (int Home, int Away, double Confidence)[] predictions) =>
        new(fixture, [.. predictions.Select(p => new Prediction(p.Home, p.Away, p.Confidence))]);

    private static GameweekResponse Ok(ActionResult<GameweekResponse> result) =>
        Assert.IsType<GameweekResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);

    [Fact]
    public async Task The_most_likely_score_is_reported_for_each_fixture()
    {
        var controller = new GameweeksController(new StubGameweekPredictor(
            Predicted(FixtureAt(14, "Everton", "Ipswich Town"), (2, 1, 0.31), (1, 1, 0.22))));

        var response = Ok(await controller.Get(5));

        var fixture = Assert.Single(response.Fixtures);
        Assert.Equal("Everton", fixture.HomeTeam);
        Assert.Equal("Ipswich Town", fixture.AwayTeam);
        Assert.Equal(2, fixture.MostLikelyScore!.Home);
        Assert.Equal(1, fixture.MostLikelyScore.Away);
        Assert.Equal(0.31, fixture.MostLikelyScore.Confidence);
    }

    [Fact]
    public async Task The_gameweek_that_was_asked_for_is_echoed_back()
    {
        var predictor = new StubGameweekPredictor(
            Predicted(FixtureAt(14, "Everton", "Ipswich Town"), (1, 0, 0.3)));
        var controller = new GameweeksController(predictor);

        var response = Ok(await controller.Get(5));

        Assert.Equal(5, response.Gameweek);
        Assert.Equal(5, predictor.Asked);
    }

    [Fact]
    public async Task Fixtures_keep_the_order_the_domain_gave_them()
    {
        var controller = new GameweeksController(new StubGameweekPredictor(
            Predicted(FixtureAt(11, "Tottenham Hotspur", "Aston Villa"), (1, 1, 0.2)),
            Predicted(FixtureAt(14, "Everton", "Ipswich Town"), (2, 0, 0.3)),
            Predicted(FixtureAt(16, "Nottingham Forest", "Coventry City"), (0, 1, 0.4))));

        var response = Ok(await controller.Get(5));

        Assert.Equal(
            ["Tottenham Hotspur", "Everton", "Nottingham Forest"],
            response.Fixtures.Select(f => f.HomeTeam));
    }

    [Fact]
    public async Task A_gameweek_with_no_fixtures_is_not_found()
    {
        var controller = new GameweeksController(new StubGameweekPredictor());

        var result = await controller.Get(99);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task A_fixture_Jev_could_not_call_is_reported_without_a_score()
    {
        var controller = new GameweeksController(new StubGameweekPredictor(
            Predicted(FixtureAt(14, "Everton", "Ipswich Town"))));

        var response = Ok(await controller.Get(5));

        var fixture = Assert.Single(response.Fixtures);
        Assert.Null(fixture.MostLikelyScore);
        Assert.Equal("Everton", fixture.HomeTeam);
    }
}
