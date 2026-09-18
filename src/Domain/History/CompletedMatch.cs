namespace Domain.History;

/// <summary>
/// A finished match, reduced to what bears on a future scoreline.
/// Lineups, substitutions and commentary are deliberately absent: they dominate the
/// source dataset's size and say little about how many goals the next match will hold.
/// </summary>
public sealed record CompletedMatch(
    DateTimeOffset KickoffUtc,
    TeamPerformance Home,
    TeamPerformance Away)
{
    public bool Involves(string team) =>
        string.Equals(Home.Name, team, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Away.Name, team, StringComparison.OrdinalIgnoreCase);
}

public sealed record TeamPerformance(
    string Name,
    int Goals,
    int? LeaguePositionBefore,
    MatchStats? Stats);

public sealed record MatchStats(
    int Shots,
    int ShotsOnTarget,
    int ShotsOffTarget,
    int BlockedShots,
    double? ExpectedGoals,
    double? PossessionPercent);
