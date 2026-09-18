namespace External.Tests.Builders;

using Domain.History;

internal static class AMatch
{
    /// <summary>A completed match carrying the shooting figures the state is expected to relay.</summary>
    public static CompletedMatch PlayedOn(
        int day, string home, string away, int homeGoals = 2, int awayGoals = 1) =>
        new(new DateTimeOffset(2026, 8, day, 14, 0, 0, TimeSpan.Zero),
            new TeamPerformance(home, homeGoals, 5, new MatchStats(14, 5, 6, 3, 1.62, 55.4)),
            new TeamPerformance(away, awayGoals, 12, new MatchStats(9, 3, 4, 2, 0.91, 44.6)));
}
