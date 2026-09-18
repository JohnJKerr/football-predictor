namespace External.Jev;

/// <summary>Credentials and endpoint for the Jev API, supplied by configuration or user secrets.</summary>
public interface IJevSettings
{
    string ApiKey { get; }
    Uri BaseAddress { get; }
    string Model { get; }
}
