namespace Domain.History;

using Domain.Model;

/// <summary>
/// How the league behaves over the long run.
/// <para>
/// Jev has no way to know that roughly a quarter of Premier League matches are drawn, and
/// backtesting showed it under-weighting draws and over-weighting away wins. Sending the
/// base rates gives it the anchor its own answers should sit against.
/// </para>
/// </summary>
public sealed record LeagueBaseRates(
    int Matches,
    double HomeWin,
    double Draw,
    double AwayWin,
    double GoalsPerMatch,
    double OverTwoAndAHalfGoals,
    double BothTeamsToScore)
{
    public static LeagueBaseRates None { get; } = new(0, 0, 0, 0, 0, 0, 0);

    public static LeagueBaseRates From(IReadOnlyCollection<PriorResult> results)
    {
        if (results.Count == 0) return None;

        var total = (double)results.Count;

        return new LeagueBaseRates(
            results.Count,
            Share(results, r => r.Outcome == Outcome.HomeWin, total),
            Share(results, r => r.Outcome == Outcome.Draw, total),
            Share(results, r => r.Outcome == Outcome.AwayWin, total),
            Rounded(results.Sum(r => r.TotalGoals) / total),
            Share(results, r => r.TotalGoals > 2.5, total),
            Share(results, r => r.BothScored, total));
    }

    private static double Share(
        IEnumerable<PriorResult> results, Func<PriorResult, bool> matching, double total) =>
        Rounded(results.Count(matching) / total);

    // Sent to Jev as text, so more than three places is noise.
    private static double Rounded(double value) => Math.Round(value, 3);
}
