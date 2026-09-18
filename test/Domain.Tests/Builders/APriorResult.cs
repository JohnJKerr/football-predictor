namespace Domain.Tests.Builders;

using Domain.History;

internal static class APriorResult
{
    public static PriorResult Of(string home, int homeScore, int awayScore, string away = "Everton") =>
        new(new DateOnly(2024, 5, 1), home, homeScore, away, awayScore);

    /// <summary>A run of matches with a given result, for shaping base rates in a test.</summary>
    public static IEnumerable<PriorResult> Repeated(
        int times, int homeScore, int awayScore, string home = "Arsenal", string away = "Everton") =>
        Enumerable.Range(0, times).Select(_ => Of(home, homeScore, awayScore, away));
}
