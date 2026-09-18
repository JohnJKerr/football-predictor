namespace Domain.Schedule;

using Domain.Model;

/// <summary>
/// Places the published fixture list into gameweeks.
/// <para>
/// The source data carries no matchweek field, so a gameweek is derived as a block of
/// <see cref="MatchesPerGameweek"/> consecutive fixtures in kickoff order. For a 20-club
/// league that yields each club exactly once per block; <c>GameweekScheduleFileTests</c>
/// asserts this holds across the real season.
/// </para>
/// </summary>
public sealed class GameweekSchedule(IFixtureSource source) : IGameweekSchedule
{
    /// <summary>Half the number of clubs in the league: every club plays once per gameweek.</summary>
    public const int MatchesPerGameweek = 10;

    public async Task<IReadOnlyList<Fixture>> GetGameweekAsync(
        int gameweek, CancellationToken cancellationToken = default)
    {
        if (gameweek < 1) return [];

        var listings = await source.GetAllAsync(cancellationToken);

        return
        [
            .. listings
                // Ties are broken on Id so that simultaneous kickoffs group deterministically.
                .OrderBy(f => f.KickoffUtc)
                .ThenBy(f => f.Id, StringComparer.Ordinal)
                .Skip((gameweek - 1) * MatchesPerGameweek)
                .Take(MatchesPerGameweek)
                .Select(f => new Fixture(f.Id, gameweek, f.KickoffUtc, f.HomeTeam, f.AwayTeam))
        ];
    }
}
