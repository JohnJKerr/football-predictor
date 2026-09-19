namespace Domain.Calibration;

using Domain.History;

/// <summary>
/// Gathers what Jev said about recent gameweeks and what actually happened, so it can be
/// shown its own record and judge how confident to be.
/// <para>
/// Only played fixtures appear: a prediction whose result is not in yet says nothing about
/// accuracy. Most recent gameweek first, because that is the record worth weighing most.
/// </para>
/// </summary>
public sealed class CalibrationFeedback(
    IPredictionLog log,
    IMatchHistory history,
    int gameweeks = CalibrationFeedback.DefaultGameweeks)
    : ICalibrationFeedback
{
    /// <summary>Enough to show a pattern without crowding out this season's form.</summary>
    public const int DefaultGameweeks = 3;

    public async Task<IReadOnlyList<PredictionOutcome>> BeforeAsync(
        int gameweek, CancellationToken cancellationToken = default)
    {
        var played = await history.GetCompletedAsync(cancellationToken);
        var results = played.ToDictionary(
            m => (m.Home.Name, m.Away.Name),
            m => (m.Home.Goals, m.Away.Goals));

        var record = new List<PredictionOutcome>();

        for (var past = gameweek - 1; past >= 1 && past > gameweek - 1 - gameweeks; past--)
        {
            foreach (var logged in await log.ForGameweekAsync(past, cancellationToken))
            {
                if (!results.TryGetValue((logged.HomeTeam, logged.AwayTeam), out var result)) continue;

                record.Add(new PredictionOutcome(
                    logged.Gameweek,
                    logged.HomeTeam,
                    logged.AwayTeam,
                    logged.Outcome,
                    logged.Scoreline,
                    result.Item1,
                    result.Item2));
            }
        }

        return record;
    }
}
