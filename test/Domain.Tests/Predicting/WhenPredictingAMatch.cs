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
        var predictions = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([(0, 0), (2, 1), (1, 0)], predictions.Select(p => (p.HomeScore, p.AwayScore)));
    }

    [Fact]
    public async Task The_likeliest_scoreline_carries_Jevs_own_probability_as_its_confidence()
    {
        // Arrange
        var jev = GivenJev.Returning((2, 1, 0.35), (1, 0, 0.20)).Build();

        // Act
        var predictions = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(new Prediction(2, 1, 0.35), predictions[0]);
    }

    [Fact]
    public async Task A_scoreline_Jev_gave_no_chance_of_happening_is_omitted()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.60), (4, 4, 0.0), (2, 1, 0.40)).Build();

        // Act
        var predictions = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.DoesNotContain(predictions, p => p is { HomeScore: 4, AwayScore: 4 });
    }

    [Fact]
    public async Task Only_scorelines_Jev_gave_a_chance_of_happening_are_returned()
    {
        // Arrange
        var jev = GivenJev.Returning((1, 0, 0.60), (4, 4, 0.0), (2, 1, 0.40)).Build();

        // Act
        var predictions = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(2, predictions.Count);
    }

    [Fact]
    public async Task The_catch_all_bucket_is_excluded_because_it_is_not_a_scoreline()
    {
        // Arrange
        // "other" holds the most mass, but it names no result we could predict.
        var jev = GivenJev.Returning((1, 0, 0.30)).AndInTheCatchAll(0.70).Build();

        // Act
        var predictions = await new MatchPredictor(jev).PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal([new Prediction(1, 0, 0.30)], predictions);
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
