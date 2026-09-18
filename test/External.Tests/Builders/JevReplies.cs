namespace External.Tests.Builders;

internal static class JevReplies
{
    public const string AnyValid = """
        { "model": "jev-latest",
          "answers": {
            "scoreline": { "type": "choice", "choice": "1-0",
                           "probabilities": { "1-0": 1.0 }, "confidence": 0.5 },
            "outcome": { "type": "choice", "choice": "home_win",
                         "probabilities": { "home_win": 1.0, "draw": 0.0, "away_win": 0.0 },
                         "confidence": 0.5 },
            "over_two_and_a_half_goals": { "type": "noul", "noul": 0.5 },
            "both_teams_to_score": { "type": "noul", "noul": 0.5 }
          },
          "usage": { "input_tokens": 1, "output_tokens": 1 } }
        """;

    public const string ARankedDistribution = """
        {
          "model": "jev-latest",
          "answers": {
            "scoreline": {
              "type": "choice",
              "choice": "2-1",
              "probabilities": { "2-1": 0.31, "1-1": 0.22, "0-0": 0.07, "other": 0.40 },
              "confidence": 0.64
            },
            "outcome": {
              "type": "choice",
              "choice": "away_win",
              "probabilities": { "home_win": 0.24, "draw": 0.31, "away_win": 0.45 },
              "confidence": 0.58
            },
            "over_two_and_a_half_goals": { "type": "noul", "noul": 0.62 },
            "both_teams_to_score": { "type": "noul", "noul": 0.71 }
          },
          "usage": { "input_tokens": 12000, "output_tokens": 64 }
        }
        """;

    public const string WithNoAnswer = """{ "model": "jev-latest", "answers": {} }""";

    /// <summary>A scoreline came back, but none of the atomic questions did.</summary>
    public const string WithOnlyAScoreline = """
        { "model": "jev-latest",
          "answers": { "scoreline": { "type": "choice", "choice": "1-0",
                       "probabilities": { "1-0": 1.0 }, "confidence": 0.5 } },
          "usage": { "input_tokens": 1, "output_tokens": 1 } }
        """;
}
