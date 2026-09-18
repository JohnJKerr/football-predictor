namespace Domain.Tests.Builders;

using Domain.History;

internal static class AMatch
{
    /// <summary>A completed match in August 2026, identified in assertions by its day.</summary>
    public static CompletedMatch PlayedOn(
        int day, string home, string away, int homeGoals = 1, int awayGoals = 0) =>
        new(new DateTimeOffset(2026, 8, day, 14, 0, 0, TimeSpan.Zero),
            new TeamPerformance(home, homeGoals, null, null),
            new TeamPerformance(away, awayGoals, null, null));

    public static CompletedMatch PlayedAt(
        DateTimeOffset kickoff, string home, string away, int homeGoals = 1, int awayGoals = 0) =>
        new(kickoff,
            new TeamPerformance(home, homeGoals, null, null),
            new TeamPerformance(away, awayGoals, null, null));
}
