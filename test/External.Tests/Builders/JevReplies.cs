namespace External.Tests.Builders;

internal static class JevReplies
{
    public const string AnyValid = """
        { "model": "jev-latest",
          "answers": { "scoreline": { "type": "choice", "choice": "1-0",
                       "probabilities": { "1-0": 1.0 }, "confidence": 0.5 } },
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
            }
          },
          "usage": { "input_tokens": 12000, "output_tokens": 64 }
        }
        """;

    public const string WithNoAnswer = """{ "model": "jev-latest", "answers": {} }""";
}
