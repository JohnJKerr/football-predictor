namespace External.Jev;

/// <summary>Raised when Jev's reply cannot be read as a scoreline distribution.</summary>
public sealed class JevException(string message) : Exception(message);
