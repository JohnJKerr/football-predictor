namespace Domain.Calibration;

using Domain.Model;
using Domain.Predicting;

/// <summary>What we told Jev's answer was for one fixture, kept so it can be shown back later.</summary>
public sealed record LoggedPrediction(
    int Gameweek,
    string HomeTeam,
    string AwayTeam,
    OutcomeProbabilities Outcome,
    Prediction? Scoreline);

/// <summary>Predictions already made, by gameweek. Implemented in External.</summary>
public interface IPredictionLog
{
    Task RecordAsync(
        int gameweek, IReadOnlyList<FixturePrediction> predictions, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoggedPrediction>> ForGameweekAsync(
        int gameweek, CancellationToken cancellationToken = default);
}
