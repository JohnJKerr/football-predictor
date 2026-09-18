namespace External.Tests.Jev;

using External.Tests.Builders;

public class WhenAskingJevForAScoreline
{
    [Fact]
    public async Task The_request_is_posted()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(HttpMethod.Post, jev.Request.Method);
    }

    [Fact]
    public async Task The_request_goes_to_the_system_one_endpoint()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("https://api.typesafe.ai/v1/systemone", jev.Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task The_api_key_is_sent_as_a_bearer_token()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-api-key"),
            jev.Request.Headers.Authorization);
    }

    [Fact]
    public async Task The_body_is_sent_as_json()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("application/json", jev.Request.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task The_body_carries_a_content_length_rather_than_being_chunked()
    {
        // Arrange
        // Jev is behind a gateway; a chunked upload with no length is a needless risk.
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.NotNull(jev.Request.Content!.Headers.ContentLength);
    }

    [Fact]
    public async Task The_question_is_asked_against_the_named_model()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("jev-latest", (string?)jev.Body["model"]);
    }

    [Fact]
    public async Task A_single_question_is_asked()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("scoreline", Assert.Single(jev.Body["questions"]!.AsObject()).Key);
    }

    [Fact]
    public async Task The_question_is_a_choice()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("choice", (string?)jev.Question["type"]);
    }

    [Fact]
    public async Task The_question_carries_instructions()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace((string?)jev.Question["instructions"]));
    }

    [Fact]
    public async Task Every_scoreline_in_the_grid_is_offered_plus_a_catch_all()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        // 7x7 grid of 0-6 goals per side, plus "other".
        Assert.Equal(50, jev.Criteria.Count);
    }

    [Theory]
    [InlineData("0-0")]
    [InlineData("2-1")]
    [InlineData("6-6")]
    [InlineData("other")]
    public async Task The_options_include_the_expected_key(string key)
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Contains(key, jev.Criteria.Select(o => o.Key));
    }

    [Fact]
    public async Task A_scoreline_beyond_the_grid_is_not_offered()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.DoesNotContain("7-0", jev.Criteria.Select(o => o.Key));
    }

    [Fact]
    public async Task The_catch_all_option_is_described()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace((string?)jev.Criteria["other"]));
    }
}
