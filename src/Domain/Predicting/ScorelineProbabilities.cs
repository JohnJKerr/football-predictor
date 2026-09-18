namespace Domain.Predicting;

using Domain.Model;

/// <summary>Jev's probability distribution across the candidate scorelines for one fixture.</summary>
public sealed record ScorelineProbabilities(
    IReadOnlyDictionary<Scoreline, double> ByScoreline,
    double Other,
    double Confidence);
