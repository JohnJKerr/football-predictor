namespace Api.Models;

public sealed record GameweekResponse(int Gameweek, IReadOnlyList<FixturePredictionResponse> Fixtures);

public sealed record FixturePredictionResponse(
    string FixtureId,
    DateTimeOffset KickoffUtc,
    string HomeTeam,
    string AwayTeam,
    ScoreResponse? MostLikelyScore,
    OutcomeResponse? Outcome,
    double OverTwoAndAHalfGoals,
    double BothTeamsToScore);

/// <summary><paramref name="Confidence"/> is Jev's probability for this exact scoreline.</summary>
public sealed record ScoreResponse(int Home, int Away, double Confidence);

/// <summary>
/// Which way the match is likely to go. Three options rather than fifty, so this carries
/// far more probability than any single scoreline can.
/// </summary>
public sealed record OutcomeResponse(
    string Result,
    double HomeWin,
    double Draw,
    double AwayWin,
    double Confidence);
