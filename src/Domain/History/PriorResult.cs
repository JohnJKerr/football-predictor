namespace Domain.History;

/// <summary>
/// A finished match from a previous season. Only the result is recorded: these exist to
/// establish base rates and long-run club records, not to be replayed in detail.
/// </summary>
public sealed record PriorResult(
    DateOnly Date,
    string HomeTeam,
    int HomeScore,
    string AwayTeam,
    int AwayScore)
{
    public int TotalGoals => HomeScore + AwayScore;

    public bool BothScored => HomeScore > 0 && AwayScore > 0;

    public Model.Outcome Outcome => HomeScore.CompareTo(AwayScore) switch
    {
        > 0 => Model.Outcome.HomeWin,
        < 0 => Model.Outcome.AwayWin,
        _ => Model.Outcome.Draw,
    };
}
