namespace Domain.Tests.Builders;

using Domain.History;

internal static class AMatch
{
    /// <summary>A completed match in August 2026, identified in assertions by its day.</summary>
    public static CompletedMatch PlayedOn(
        int day, string home, string away, int homeGoals = 1, int awayGoals = 0) =>
        new(On(day),
            new TeamPerformance(home, homeGoals, null, null),
            new TeamPerformance(away, awayGoals, null, null));

    public static CompletedMatch PlayedAt(
        DateTimeOffset kickoff, string home, string away, int homeGoals = 1, int awayGoals = 0) =>
        new(kickoff,
            new TeamPerformance(home, homeGoals, null, null),
            new TeamPerformance(away, awayGoals, null, null));

    /// <summary>The same match carrying expected goals, which only some matches have.</summary>
    public static CompletedMatch PlayedOnWithXg(
        int day, string home, string away, double homeXg, double awayXg,
        int homeGoals = 1, int awayGoals = 0) =>
        new(On(day),
            new TeamPerformance(home, homeGoals, null, Shooting(homeXg)),
            new TeamPerformance(away, awayGoals, null, Shooting(awayXg)));

    private static DateTimeOffset On(int day) => new(2026, 8, day, 14, 0, 0, TimeSpan.Zero);

    private static MatchStats Shooting(double xg) => new(10, 4, 4, 2, xg, 50.0);
}
