namespace External.Jev;

public sealed class JevSettings : IJevSettings
{
    public const string SectionName = "Jev";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.typesafe.ai";

    public string Model { get; set; } = "jev-latest";

    Uri IJevSettings.BaseAddress => new(BaseUrl);
}
