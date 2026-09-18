namespace Domain.Tests.Predicting;

using Domain.Model;
using Domain.Predicting;
using Domain.Tests.Builders;

public class WhenPredictingAMatch
{
    [Fact]
    public async Task Candidate_scorelines_are_ranked_most_likely_first()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.20), (2, 1, 0.35), (0, 0, 0.45)).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([(0, 0), (2, 1), (1, 0)], prediction.Scorelines.Select(p => (p.HomeScore, p.AwayScore)));
    }

    [Fact]
    public async Task The_likeliest_scoreline_carries_Jevs_own_probability_as_its_confidence()
    {
        // Arrange
        var jev = GivenJev.Returning((2, 1, 0.35), (1, 0, 0.20)).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(new Prediction(2, 1, 0.35), prediction.Scorelines[0]);
    }

    [Fact]
    public async Task A_scoreline_Jev_gave_no_chance_of_happening_is_omitted()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.60), (4, 4, 0.0), (2, 1, 0.40)).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.DoesNotContain(prediction.Scorelines, p => p is { HomeScore: 4, AwayScore: 4 });
    }

    [Fact]
    public async Task Only_scorelines_Jev_gave_a_chance_of_happening_are_returned()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.60), (4, 4, 0.0), (2, 1, 0.40)).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, prediction.Scorelines.Count);
    }

    [Fact]
    public async Task The_catch_all_bucket_is_excluded_because_it_is_not_a_scoreline()
    {
        // Arrange
        // "other" holds the most mass, but it names no result we could predict.
        var jev = GivenJev.Returning((1, 0, 0.30)).AndInTheCatchAll(0.70).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([new Prediction(1, 0, 0.30)], prediction.Scorelines);
    }

    [Fact]
    public async Task The_outcome_Jev_weighed_up_is_passed_through()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.3))
            .AndOutcome(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(Outcome.AwayWin, prediction.Outcome.MostLikely);
    }

    [Fact]
    public async Task Jevs_confidence_in_the_outcome_is_passed_through()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.3))
            .AndOutcome(home: 0.24, draw: 0.31, away: 0.45, confidence: 0.58).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.58, prediction.Outcome.Confidence);
    }

    [Fact]
    public async Task The_chance_of_three_or_more_goals_is_passed_through()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.3)).AndGoals(0.62, 0.71).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.62, prediction.OverTwoAndAHalfGoals);
    }

    [Fact]
    public async Task The_chance_of_both_clubs_scoring_is_passed_through()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.3)).AndGoals(0.62, 0.71).Build();

        // Act
        var prediction = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.71, prediction.BothTeamsToScore);
    }

    [Fact]
    public async Task Jev_is_asked_about_the_fixture_it_was_given()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 1.0)).Build();

        // Act
        await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Same(AFixture.Upcoming, jev.Asked);
    }
}
