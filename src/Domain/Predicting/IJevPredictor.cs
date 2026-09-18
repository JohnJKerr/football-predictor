namespace Domain.Predicting;

using Domain.Model;

/// <summary>The seam onto Jev. Implemented in External; the domain never speaks HTTP.</summary>
public interface IJevPredictor
{
    Task<MatchForecast> PredictAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
