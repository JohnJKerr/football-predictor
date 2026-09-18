namespace External.Tests.Builders;

using Domain.Model;

internal static class AFixture
{
    public static Fixture Upcoming { get; } = new(
        Id: "espn:401879270",
        Gameweek: 5,
        KickoffUtc: new DateTimeOffset(2026, 9, 19, 16, 30, 0, TimeSpan.Zero),
        HomeTeam: "Nottingham Forest",
        AwayTeam: "Coventry City");
}
