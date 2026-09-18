namespace External.Tests.Builders;

using System.Net;
using Domain.History;
using Domain.Model;
using Domain.Predicting;
using External.Jev;

/// <summary>Wires a <see cref="JevPredictor"/> onto a fake transport that records the request.</summary>
internal sealed class GivenJev
{
    private string reply = JevReplies.AnyValid;
    private CompletedMatch[] history = [];
    private HttpStatusCode? failure;
    private IJevSettings settings = new TestJevSettings();

    public static GivenJev Asked() => new();

    public static GivenJev WithHistory(params CompletedMatch[] matches) =>
        new() { history = matches };

    public static GivenJev Replying(string reply) => new() { reply = reply };

    public static GivenJev Failing(HttpStatusCode status) => new() { failure = status };

    public GivenJev AndHistory(params CompletedMatch[] matches)
    {
        history = matches;
        return this;
    }

    public JevUnderTest Build()
    {
        HttpMessageHandler handler = failure is { } status
            ? new StatusHandler(status)
            : new RecordingHandler(reply);

        return new JevUnderTest(
            new JevPredictor(new HttpClient(handler), settings, new StubRelevantHistory(history)),
            handler as RecordingHandler,
            history);
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status));
    }

    private sealed class TestJevSettings : IJevSettings
    {
        public string ApiKey => "test-api-key";

        public Uri BaseAddress => new("https://api.typesafe.ai");

        public string Model => "jev-latest";
    }

    private sealed class StubRelevantHistory(CompletedMatch[] matches) : IRelevantHistory
    {
        public Fixture? Asked { get; private set; }

        public Task<IReadOnlyList<CompletedMatch>> ForAsync(
            Fixture fixture, CancellationToken cancellationToken = default)
        {
            Asked = fixture;
            return Task.FromResult<IReadOnlyList<CompletedMatch>>(matches);
        }
    }

    /// <summary>Exposes what the predictor sent, so assertions can read one thing each.</summary>
    internal sealed class JevUnderTest(
        JevPredictor predictor, RecordingHandler? handler, IReadOnlyList<CompletedMatch> given)
    {
        public IReadOnlyList<CompletedMatch> GivenHistory => given;

        public Task<MatchForecast> PredictAsync(Fixture fixture)
            => predictor.PredictAsync(fixture);

        public HttpRequestMessage Request => handler!.Request!;

        public System.Text.Json.Nodes.JsonNode Body => handler!.Body;

        public System.Text.Json.Nodes.JsonObject Questions => Body["questions"]!.AsObject();

        public System.Text.Json.Nodes.JsonNode Question => Questions["scoreline"]!;

        public System.Text.Json.Nodes.JsonNode QuestionNamed(string key) => Questions[key]!;

        public System.Text.Json.Nodes.JsonObject Criteria => Question["criteria"]!.AsObject();

        public System.Text.Json.Nodes.JsonNode Fixture => Body["state"]!["fixture"]!;

        public System.Text.Json.Nodes.JsonArray History => Body["state"]!["history"]!.AsArray();

        public long RequestBytes => Request.Content!.Headers.ContentLength!.Value;
    }
}
