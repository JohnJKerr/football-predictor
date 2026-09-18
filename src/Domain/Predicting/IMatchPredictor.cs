namespace Domain.Predicting;

using Domain.Model;

public interface IMatchPredictor
{
    Task<IReadOnlyList<Prediction>> PredictAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
