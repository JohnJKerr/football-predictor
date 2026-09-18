namespace Domain.History;

using Domain.Model;

/// <summary>
/// How clubs fare in the season they come up, pooled across every promotion in the data.
/// <para>
/// A club promoted into the league has no record of its own. Sending nothing leaves Jev to
/// guess; sending zeroes reads as a club that played and never won. This is the reference
/// class such a club actually belongs to.
/// </para>
/// </summary>
public sealed record PromotedSideProfile(ClubRecord AtHome, ClubRecord AwayFromHome)
{
    public const string ClubName = "clubs promoted into the league";

    public static PromotedSideProfile? From(IReadOnlyList<PriorResult> results)
    {
        var promotions = PromotedSides.In(results)
            .ToHashSet();

        if (promotions.Count == 0) return null;

        var home = results.Where(r => promotions.Contains(new Promotion(r.HomeTeam, r.Season))).ToList();
        var away = results.Where(r => promotions.Contains(new Promotion(r.AwayTeam, r.Season))).ToList();

        if (home.Count == 0 && away.Count == 0) return null;

        return new PromotedSideProfile(
            new ClubRecord(
                ClubName,
                home.Count,
                home.Count(r => r.Outcome == Outcome.HomeWin),
                home.Count(r => r.Outcome == Outcome.Draw),
                home.Count(r => r.Outcome == Outcome.AwayWin),
                home.Sum(r => r.HomeScore),
                home.Sum(r => r.AwayScore),
                RecordBasis.PromotedSides),
            new ClubRecord(
                ClubName,
                away.Count,
                away.Count(r => r.Outcome == Outcome.AwayWin),
                away.Count(r => r.Outcome == Outcome.Draw),
                away.Count(r => r.Outcome == Outcome.HomeWin),
                away.Sum(r => r.AwayScore),
                away.Sum(r => r.HomeScore),
                RecordBasis.PromotedSides));
    }
}
