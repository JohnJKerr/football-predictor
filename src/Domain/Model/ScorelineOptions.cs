namespace Domain.Model;

/// <summary>
/// The candidate scorelines we ask Jev to distribute probability across.
/// Jev's choice primitive allows up to 255 options, so we send the whole grid
/// rather than a shortlist, plus a catch-all for anything beyond it.
/// </summary>
public static class ScorelineOptions
{
    /// <summary>Highest goal tally given its own option; anything above falls into <see cref="OtherKey"/>.</summary>
    public const int MaxGoals = 6;

    public const string OtherKey = "other";

    public static string OtherDescription =>
        $"Any result where either side scores more than {MaxGoals} goals.";

    public static IReadOnlyList<Scoreline> All { get; } =
    [
        .. from home in Enumerable.Range(0, MaxGoals + 1)
           from away in Enumerable.Range(0, MaxGoals + 1)
           select new Scoreline(home, away)
    ];
}
