namespace External.Tests.Jev;

using static External.Tests.Jev.JevScenario;

public class WhenAskingJevForAScoreline
{
    [Fact]
    public async Task The_request_is_posted_to_the_system_one_endpoint_with_a_bearer_token()
    {
        var (predictor, handler) = Build();

        await predictor.PredictAsync(Upcoming);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.typesafe.ai/v1/systemone", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("test-api-key", handler.Request.Headers.Authorization.Parameter);
        Assert.Equal("application/json", handler.Request.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task The_body_carries_a_content_length_rather_than_being_chunked()
    {
        // Jev is behind a gateway; a chunked upload with no length is a needless risk.
        var (predictor, handler) = Build();

        await predictor.PredictAsync(Upcoming);

        Assert.NotNull(handler.Request!.Content!.Headers.ContentLength);
    }

    [Fact]
    public async Task A_single_scoreline_choice_question_is_asked_against_the_named_model()
    {
        var (predictor, handler) = Build();

        await predictor.PredictAsync(Upcoming);

        Assert.Equal("jev-latest", (string?)handler.Body["model"]);

        var questions = handler.Body["questions"]!.AsObject();
        Assert.Equal("scoreline", Assert.Single(questions).Key);
        Assert.Equal("choice", (string?)questions["scoreline"]!["type"]);
        Assert.False(string.IsNullOrWhiteSpace((string?)questions["scoreline"]!["instructions"]));
    }

    [Fact]
    public async Task Every_scoreline_in_the_grid_is_offered_as_an_option_plus_a_catch_all()
    {
        var (predictor, handler) = Build();

        await predictor.PredictAsync(Upcoming);

        var criteria = handler.Body["questions"]!["scoreline"]!["criteria"]!.AsObject();

        // 7x7 grid of 0-6 goals per side, plus "other".
        Assert.Equal(50, criteria.Count);
        Assert.Contains("0-0", criteria.Select(o => o.Key));
        Assert.Contains("2-1", criteria.Select(o => o.Key));
        Assert.Contains("6-6", criteria.Select(o => o.Key));
        Assert.DoesNotContain("7-0", criteria.Select(o => o.Key));
        Assert.False(string.IsNullOrWhiteSpace((string?)criteria["other"]));
    }
}
