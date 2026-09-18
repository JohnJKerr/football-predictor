namespace Domain.Predicting;

using Domain.Model;

public interface IMatchPredictor
{
    Task<MatchPrediction> PredictAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
