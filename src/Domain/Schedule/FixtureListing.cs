namespace Domain.Schedule;

/// <summary>A fixture as published by the source data, before it is placed in a gameweek.</summary>
public sealed record FixtureListing(
    string Id,
    DateTimeOffset KickoffUtc,
    string HomeTeam,
    string AwayTeam);
