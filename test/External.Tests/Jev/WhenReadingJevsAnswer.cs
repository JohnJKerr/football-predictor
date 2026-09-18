namespace External.Tests.Jev;

using System.Net;
using Domain.Model;
using External.Jev;
using static External.Tests.Jev.JevScenario;

public class WhenReadingJevsAnswer
{
    private const string Reply = """
        {
          "model": "jev-latest",
          "answers": {
            "scoreline": {
              "type": "choice",
              "choice": "2-1",
              "probabilities": { "2-1": 0.31, "1-1": 0.22, "0-0": 0.07, "other": 0.40 },
              "confidence": 0.64
            }
          },
          "usage": { "input_tokens": 12000, "output_tokens": 64 }
        }
        """;

    [Fact]
    public async Task Each_scoreline_Jev_named_keeps_its_probability()
    {
        var (predictor, _) = Replying(Reply);

        var result = await predictor.PredictAsync(Upcoming);

        Assert.Equal(0.31, result.ByScoreline[new Scoreline(2, 1)]);
        Assert.Equal(0.22, result.ByScoreline[new Scoreline(1, 1)]);
        Assert.Equal(0.07, result.ByScoreline[new Scoreline(0, 0)]);
    }

    [Fact]
    public async Task The_catch_all_bucket_is_kept_apart_from_the_scorelines()
    {
        var (predictor, _) = Replying(Reply);

        var result = await predictor.PredictAsync(Upcoming);

        Assert.Equal(0.40, result.Other);
        Assert.Equal(3, result.ByScoreline.Count);
    }

    [Fact]
    public async Task Jevs_confidence_in_the_distribution_is_carried_back()
    {
        var (predictor, _) = Replying(Reply);

        Assert.Equal(0.64, (await predictor.PredictAsync(Upcoming)).Confidence);
    }

    [Fact]
    public async Task An_unusable_reply_is_reported_as_a_Jev_failure()
    {
        var (predictor, _) = Replying("""{ "model": "jev-latest", "answers": {} }""");

        await Assert.ThrowsAsync<JevException>(() => predictor.PredictAsync(Upcoming));
    }

    [Fact]
    public async Task A_rejected_request_surfaces_rather_than_predicting_nonsense()
    {
        var predictor = new JevPredictor(
            new HttpClient(new StatusHandler(HttpStatusCode.Unauthorized)),
            new TestJevSettings(),
            new StubRelevantHistory());

        await Assert.ThrowsAsync<HttpRequestException>(() => predictor.PredictAsync(Upcoming));
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status));
    }
}
