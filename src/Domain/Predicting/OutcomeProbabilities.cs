namespace Domain.Predicting;

using Domain.Model;

/// <summary>
/// Jev's probability for each way the match could go. Asked as its own question rather than
/// summed from the scoreline grid: three options concentrate far more probability than fifty,
/// so the answer is usable where an exact scoreline is not.
/// </summary>
public sealed record OutcomeProbabilities(
    IReadOnlyDictionary<Outcome, double> ByOutcome,
    double Confidence)
{
    /// <summary>
    /// The likeliest result. A tie between a home and an away win resolves to a draw: two
    /// clubs Jev cannot separate describe a drawn match, not an arbitrary winner.
    /// </summary>
    public Outcome MostLikely
    {
        get
        {
            var home = ProbabilityOf(Outcome.HomeWin);
            var away = ProbabilityOf(Outcome.AwayWin);
            var draw = ProbabilityOf(Outcome.Draw);

            if (draw >= home && draw >= away) return Outcome.Draw;

            return home > away ? Outcome.HomeWin
                 : away > home ? Outcome.AwayWin
                 : Outcome.Draw;
        }
    }

    public double ProbabilityOf(Outcome outcome) =>
        ByOutcome.TryGetValue(outcome, out var probability) ? probability : 0;
}
