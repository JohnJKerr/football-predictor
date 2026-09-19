namespace Domain.Calibration;

using Domain.Model;
using Domain.Predicting;

/// <summary>A prediction set beside what actually happened, for showing Jev its own record.</summary>
public sealed record PredictionOutcome(
    int Gameweek,
    string HomeTeam,
    string AwayTeam,
    OutcomeProbabilities Said,
    Prediction? SaidScoreline,
    int ActualHomeScore,
    int ActualAwayScore)
{
    public Outcome ActualOutcome => new Scoreline(ActualHomeScore, ActualAwayScore).Outcome;

    public bool CalledItRight => Said.ByOutcome.Count > 0 && Said.MostLikely == ActualOutcome;
}

public interface ICalibrationFeedback
{
    /// <summary>Predictions from the gameweeks before this one, paired with the results.</summary>
    Task<IReadOnlyList<PredictionOutcome>> BeforeAsync(
        int gameweek, CancellationToken cancellationToken = default);
}
