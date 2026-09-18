namespace Domain.Predicting;

using Domain.Model;

/// <summary>A fixture together with what we predict for it.</summary>
public sealed record FixturePrediction(Fixture Fixture, MatchPrediction Prediction)
{
    /// <summary>The likeliest scoreline, or null when Jev offered none at all.</summary>
    public Prediction? MostLikely => Prediction.MostLikely;
}
