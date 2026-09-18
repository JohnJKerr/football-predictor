namespace External.Tests.Jev;

using External.Jev;

internal sealed class TestJevSettings : IJevSettings
{
    public string ApiKey { get; init; } = "test-api-key";

    public Uri BaseAddress { get; init; } = new("https://api.typesafe.ai");

    public string Model { get; init; } = "jev-latest";
}
