namespace External.Tests.Jev;

using System.Net;
using System.Text.Json.Nodes;

/// <summary>Captures the outgoing request so tests can assert its shape, and replays a canned reply.</summary>
internal sealed class RecordingHandler(string responseJson) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    public JsonNode Body { get; private set; } = new JsonObject();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        var raw = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
        Body = JsonNode.Parse(raw) ?? new JsonObject();

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
