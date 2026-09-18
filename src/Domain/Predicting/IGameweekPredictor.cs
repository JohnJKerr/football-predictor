namespace Domain.Predicting;

public interface IGameweekPredictor
{
    Task<IReadOnlyList<FixturePrediction>> PredictAsync(
        int gameweek, CancellationToken cancellationToken = default);
}
