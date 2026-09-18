namespace Domain.Model;

/// <summary>A scheduled or completed match in the season.</summary>
public sealed record Fixture(
    string Id,
    int Gameweek,
    DateTimeOffset KickoffUtc,
    string HomeTeam,
    string AwayTeam);
