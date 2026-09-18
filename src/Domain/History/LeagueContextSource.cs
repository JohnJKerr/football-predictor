namespace Domain.History;

using Domain.Model;

public sealed class LeagueContextSource(IPriorSeasons seasons) : ILeagueContext
{
    public async Task<LeagueContext> ForAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var results = await seasons.GetAsync(cancellationToken);

        // A club promoted into the league has no record of its own, so it stands in for the
        // class it belongs to. Only the record is borrowed: meetings that never happened are
        // not invented.
        var promoted = PromotedSideProfile.From(results);

        return new LeagueContext(
            LeagueBaseRates.From(results),
            AtHome(results, fixture.HomeTeam) ?? StandIn(promoted?.AtHome, fixture.HomeTeam),
            AwayFromHome(results, fixture.AwayTeam) ?? StandIn(promoted?.AwayFromHome, fixture.AwayTeam),
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

    /// <summary>Keeps the club's own name, so the record says who it is about as well as where it came from.</summary>
    private static ClubRecord? StandIn(ClubRecord? reference, string club) =>
        reference is null ? null : reference with { Club = club };

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
