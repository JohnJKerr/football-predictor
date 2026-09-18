namespace Domain.Tests.Builders;

using Domain.Model;

internal static class AFixture
{
    /// <summary>The fixture under question in most domain tests.</summary>
    public static Fixture Upcoming { get; } = new(
        Id: "espn:401879270",
        Gameweek: 5,
        KickoffUtc: new DateTimeOffset(2026, 9, 19, 16, 30, 0, TimeSpan.Zero),
        HomeTeam: "Nottingham Forest",
        AwayTeam: "Coventry City");

    public static Fixture KickingOffAt(int hour, string home, string away) => new(
        Id: $"espn:{hour}",
        Gameweek: 5,
        KickoffUtc: new DateTimeOffset(2026, 9, 19, hour, 0, 0, TimeSpan.Zero),
        HomeTeam: home,
        AwayTeam: away);
}
