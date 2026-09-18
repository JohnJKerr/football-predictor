namespace Domain.Predicting;

using Domain.Model;

/// <summary>A fixture together with the candidate results for it, most likely first.</summary>
public sealed record FixturePrediction(Fixture Fixture, IReadOnlyList<Prediction> Predictions)
{
    /// <summary>The likeliest result, or null when Jev offered no scoreline at all.</summary>
    public Prediction? MostLikely => Predictions.Count > 0 ? Predictions[0] : null;
}
