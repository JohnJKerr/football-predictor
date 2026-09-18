namespace External.Tests.Jev;

using Domain.History;
using Domain.Model;
using External.Jev;

/// <summary>Shared setup for the Jev behaviours: a fixture, a stubbed reply, and a wired predictor.</summary>
internal static class JevScenario
{
    public const string AnyValidReply = """
        { "model": "jev-latest",
          "answers": { "scoreline": { "type": "choice", "choice": "1-0",
                       "probabilities": { "1-0": 1.0 }, "confidence": 0.5 } },
          "usage": { "input_tokens": 1, "output_tokens": 1 } }
        """;

    public static readonly Fixture Upcoming = new(
        Id: "espn:401879270",
        Gameweek: 5,
        KickoffUtc: new DateTimeOffset(2026, 9, 19, 16, 30, 0, TimeSpan.Zero),
        HomeTeam: "Nottingham Forest",
        AwayTeam: "Coventry City");

    public static (JevPredictor Predictor, RecordingHandler Handler) Build(
        params CompletedMatch[] history) => Replying(AnyValidReply, history);

    public static (JevPredictor Predictor, RecordingHandler Handler) Replying(
        string reply, params CompletedMatch[] history)
    {
        var handler = new RecordingHandler(reply);
        var predictor = new JevPredictor(
            new HttpClient(handler), new TestJevSettings(), new StubRelevantHistory(history));

        return (predictor, handler);
    }

    public static CompletedMatch Match(
        int day, string home, string away, int homeGoals = 2, int awayGoals = 1) =>
        StubRelevantHistory.Match(day, home, away, homeGoals, awayGoals);
}
