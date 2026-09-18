namespace Api.Models;

public sealed record GameweekResponse(int Gameweek, IReadOnlyList<FixturePredictionResponse> Fixtures);

public sealed record FixturePredictionResponse(
    string FixtureId,
    DateTimeOffset KickoffUtc,
    string HomeTeam,
    string AwayTeam,
    ScoreResponse? MostLikelyScore);

/// <summary><paramref name="Confidence"/> is Jev's probability for this exact scoreline.</summary>
public sealed record ScoreResponse(int Home, int Away, double Confidence);
