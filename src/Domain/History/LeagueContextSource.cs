namespace Domain.History;

using Domain.Model;

public sealed class LeagueContextSource(IPriorSeasons seasons) : ILeagueContext
{
    public async Task<LeagueContext> ForAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var results = await seasons.GetAsync(cancellationToken);

        return new LeagueContext(
            LeagueBaseRates.From(results),
            AtHome(results, fixture.HomeTeam),
            AwayFromHome(results, fixture.AwayTeam),
            Meetings(results, fixture.HomeTeam, fixture.AwayTeam));
    }

    private static ClubRecord? AtHome(IReadOnlyList<PriorResult> results, string club)
    {
        var played = results.Where(r => Same(r.HomeTeam, club)).ToList();
        if (played.Count == 0) return null;

        return new ClubRecord(
            club,
            played.Count,
            played.Count(r => r.Outcome == Outcome.HomeWin),
            played.Count(r => r.Outcome == Outcome.Draw),
            played.Count(r => r.Outcome == Outcome.AwayWin),
            played.Sum(r => r.HomeScore),
            played.Sum(r => r.AwayScore));
    }

    private static ClubRecord? AwayFromHome(IReadOnlyList<PriorResult> results, string club)
    {
        var played = results.Where(r => Same(r.AwayTeam, club)).ToList();
        if (played.Count == 0) return null;

        return new ClubRecord(
            club,
            played.Count,
            played.Count(r => r.Outcome == Outcome.AwayWin),
            played.Count(r => r.Outcome == Outcome.Draw),
            played.Count(r => r.Outcome == Outcome.HomeWin),
            played.Sum(r => r.AwayScore),
            played.Sum(r => r.HomeScore));
    }

    private static HeadToHead? Meetings(IReadOnlyList<PriorResult> results, string home, string away)
    {
        var met = results
            .Where(r => (Same(r.HomeTeam, home) && Same(r.AwayTeam, away))
                     || (Same(r.HomeTeam, away) && Same(r.AwayTeam, home)))
            .ToList();

        if (met.Count == 0) return null;

        // Counted for the club that will be at home this time, wherever the meeting was played.
        var won = met.Count(r => Same(r.HomeTeam, home)
            ? r.Outcome == Outcome.HomeWin
            : r.Outcome == Outcome.AwayWin);

        var drawn = met.Count(r => r.Outcome == Outcome.Draw);

        return new HeadToHead(met.Count, won, drawn, met.Count - won - drawn);
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
