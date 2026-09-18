namespace Domain.Model;

/// <summary>
/// A candidate result for a fixture. <paramref name="Confidence"/> is Jev's own
/// probability for this exact scoreline, so confidences across a fixture's
/// predictions sum to 1 (less whatever mass landed on "other").
/// </summary>
public sealed record Prediction(int HomeScore, int AwayScore, double Confidence);
