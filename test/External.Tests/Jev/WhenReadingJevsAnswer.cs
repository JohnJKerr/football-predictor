namespace External.Tests.Jev;

using System.Net;
using Domain.Model;
using External.Jev;
using External.Tests.Builders;

public class WhenReadingJevsAnswer
{
    [Theory]
    [InlineData(2, 1, 0.31)]
    [InlineData(1, 1, 0.22)]
    [InlineData(0, 0, 0.07)]
    public async Task Each_scoreline_Jev_named_keeps_its_probability(int home, int away, double expected)
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(expected, forecast.Scorelines.ByScoreline[new Scoreline(home, away)]);
    }

    [Fact]
    public async Task The_catch_all_bucket_keeps_its_own_probability()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.40, forecast.Scorelines.Other);
    }

    [Fact]
    public async Task The_catch_all_bucket_is_kept_out_of_the_scorelines()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(3, forecast.Scorelines.ByScoreline.Count);
    }

    [Fact]
    public async Task Jevs_confidence_in_the_distribution_is_carried_back()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.ARankedDistribution).Build();

        // Act
        var forecast = await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(0.64, forecast.Scorelines.Confidence);
    }

    [Fact]
    public async Task An_unusable_reply_is_reported_as_a_Jev_failure()
    {
        // Arrange
        var jev = GivenJev.Replying(JevReplies.WithNoAnswer).Build();

        // Act
        var reading = async () => await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        await Assert.ThrowsAsync<JevException>(reading);
    }

    [Fact]
    public async Task A_rejected_request_surfaces_rather_than_predicting_nonsense()
    {
        // Arrange
        var jev = GivenJev.Failing(HttpStatusCode.Unauthorized).Build();

        // Act
        var reading = async () => await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(reading);
    }
}
