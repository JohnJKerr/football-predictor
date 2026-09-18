namespace Domain.Predicting;

using Domain.Model;

/// <summary>
/// What we will say about one fixture: the candidate scorelines most likely first, and the
/// narrower judgements that carry more certainty than any single scoreline can.
/// </summary>
public sealed record MatchPrediction(
    IReadOnlyList<Prediction> Scorelines,
    OutcomeProbabilities Outcome,
    double OverTwoAndAHalfGoals,
    double BothTeamsToScore)
{
    /// <summary>The likeliest scoreline, or null when Jev offered none at all.</summary>
    public Prediction? MostLikely => Scorelines.Count > 0 ? Scorelines[0] : null;
}
