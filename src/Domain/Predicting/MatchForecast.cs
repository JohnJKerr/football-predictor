namespace Domain.Predicting;

/// <summary>
/// Everything Jev was asked about one fixture, answered in a single request.
/// <para>
/// The scoreline is the headline but the least certain answer: fifty options share the
/// probability, so the best of them rarely holds more than a fifth. The narrower questions
/// are asked alongside it because three or two options concentrate enough to act on.
/// </para>
/// </summary>
public sealed record MatchForecast(
    ScorelineProbabilities Scorelines,
    OutcomeProbabilities Outcome,
    double OverTwoAndAHalfGoals,
    double BothTeamsToScore);
