namespace Domain.History;

/// <summary>A club and the season it came up into the league.</summary>
public readonly record struct Promotion(string Club, int Season);

public static class PromotedSides
{
    /// <summary>
    /// Clubs that appear in a season but not the one before it. The earliest season in the
    /// data can never contribute: with nothing to compare against, every club looks new.
    /// </summary>
    public static IReadOnlyList<Promotion> In(IReadOnlyList<PriorResult> results)
    {
        var clubsBySeason = new Dictionary<int, HashSet<string>>();

        foreach (var result in results)
        {
            if (!clubsBySeason.TryGetValue(result.Season, out var clubs))
            {
                clubs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                clubsBySeason[result.Season] = clubs;
            }

            clubs.Add(result.HomeTeam);
            clubs.Add(result.AwayTeam);
        }

        var seasons = clubsBySeason.Keys.Order().ToList();

        return
        [
            .. from pair in seasons.Zip(seasons.Skip(1))
               from club in clubsBySeason[pair.Second].Except(clubsBySeason[pair.First])
               orderby pair.Second, club
               select new Promotion(club, pair.Second)
        ];
    }
}
