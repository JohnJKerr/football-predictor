namespace Domain.History;

using Domain.Model;

/// <summary>
/// Narrows the season to the matches bearing on one fixture: those played by either club,
/// most recent first. Jev caps its context, and the full season does not fit, so relevance
/// is decided here rather than by truncating arbitrarily at the wire.
/// </summary>
public sealed class RelevantHistory(IMatchHistory history, int maxMatchesPerClub = RelevantHistory.DefaultMaxMatchesPerClub)
    : IRelevantHistory
{
    /// <summary>A club's recent form; older results say less and cost context.</summary>
    public const int DefaultMaxMatchesPerClub = 6;

    public async Task<IReadOnlyList<CompletedMatch>> ForAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var completed = await history.GetCompletedAsync(cancellationToken);

        var selected = new List<CompletedMatch>();

        foreach (var club in new[] { fixture.HomeTeam, fixture.AwayTeam })
        {
            selected.AddRange(completed
                .Where(m => m.Involves(club))
                .OrderByDescending(m => m.KickoffUtc)
                .Take(maxMatchesPerClub));
        }

        return
        [
            // A previous meeting between the two clubs is picked up by both passes above.
            .. selected.Distinct().OrderByDescending(m => m.KickoffUtc)
        ];
    }
}
