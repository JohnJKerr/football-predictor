namespace Domain.History;

/// <summary>
/// One club's recent form stated as rates rather than as a list of matches.
/// <para>
/// The per-match history carries the same figures, but leaves Jev to aggregate them. Expected
/// goals are the part worth stating plainly: over a window this short a club's xG is a
/// steadier measure of it than its goals, which is the whole reason for sending xG at all.
/// </para>
/// <para>
/// Not every match carries xG, so the average is taken over those that do and
/// <see cref="MatchesWithXg"/> says how many that was. Averaging over the whole window would
/// read a club with missing data as a blunter one than it is.
/// </para>
/// </summary>
public sealed record FormSummary(
    string Club,
    int Matches,
    double GoalsForPerMatch,
    double GoalsAgainstPerMatch,
    int MatchesWithXg,
    double? ExpectedGoalsForPerMatch,
    double? ExpectedGoalsAgainstPerMatch)
{
    /// <summary>
    /// Summarises <paramref name="club"/>'s matches within <paramref name="window"/>, which is
    /// the history already narrowed to the fixture. Null when the club appears in none of them:
    /// a club yet to play is not a club averaging nothing.
    /// </summary>
    public static FormSummary? For(IEnumerable<CompletedMatch> window, string club)
    {
        var sides = window
            .Where(m => m.Involves(club))
            .Select(m => Same(m.Home.Name, club) ? (Own: m.Home, Other: m.Away) : (Own: m.Away, Other: m.Home))
            .ToList();

        if (sides.Count == 0) return null;

        var withXg = sides
            .Where(s => s.Own.Stats?.ExpectedGoals is not null && s.Other.Stats?.ExpectedGoals is not null)
            .ToList();

        return new FormSummary(
            club,
            sides.Count,
            Rounded(sides.Average(s => (double)s.Own.Goals)),
            Rounded(sides.Average(s => (double)s.Other.Goals)),
            withXg.Count,
            withXg.Count == 0 ? null : Rounded(withXg.Average(s => s.Own.Stats!.ExpectedGoals!.Value)),
            withXg.Count == 0 ? null : Rounded(withXg.Average(s => s.Other.Stats!.ExpectedGoals!.Value)));
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // Sent to Jev as text, so more than three places is noise.
    private static double Rounded(double value) => Math.Round(value, 3);
}
