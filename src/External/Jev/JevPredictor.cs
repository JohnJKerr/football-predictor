namespace External.Jev;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Domain.History;
using Domain.Model;
using Domain.Predicting;

/// <summary>
/// Asks Jev (TypeSafe System One) for the probability of each candidate scoreline.
/// A single <c>choice</c> question is used so Jev reports the likelihood of an exact
/// result directly, rather than us inferring it from two independent goal distributions.
/// </summary>
public sealed class JevPredictor(HttpClient httpClient, IJevSettings settings, IRelevantHistory history)
    : IJevPredictor
{
    private const string QuestionKey = "scoreline";
    private const string Endpoint = "v1/systemone";

    /// <summary>Jev accepts at most 32 KB of context per request.</summary>
    public const int MaxRequestBytes = 32 * 1024;

    private const string Instructions =
        "This is an upcoming Premier League fixture. Using the recent results in `history`, " +
        "and the fixture in `fixture`, what is the likely full-time score? Options are " +
        "written as home goals to away goals, so \"2-1\" means the home team wins by two " +
        "goals to one.";

    public async Task<ScorelineProbabilities> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var relevant = await history.ForAsync(fixture, cancellationToken);
        var payload = BuildPayloadWithinBudget(fixture, relevant);

        using var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri(settings.BaseAddress, Endpoint))
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var body = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken)
                   ?? throw new JevException("Jev returned an empty response body.");

        return ReadProbabilities(body);
    }

    /// <summary>
    /// Serialises the request, shedding the oldest matches until it fits Jev's context limit.
    /// <see cref="RelevantHistory"/> has already narrowed this to the two clubs; this is the
    /// transport's own backstop, so a long season cannot silently produce a rejected request.
    /// </summary>
    private byte[] BuildPayloadWithinBudget(Fixture fixture, IReadOnlyList<CompletedMatch> relevant)
    {
        // Most recent first, so trimming from the end sheds the least useful history.
        var considered = relevant.OrderByDescending(m => m.KickoffUtc).ToList();

        while (true)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(BuildBody(fixture, considered));

            if (payload.Length <= MaxRequestBytes || considered.Count == 0) return payload;

            // Drop roughly the overspill in one go rather than one match at a time.
            var overspill = payload.Length - MaxRequestBytes;
            var perMatch = Math.Max(1, (payload.Length / Math.Max(1, considered.Count)));
            var drop = Math.Clamp((overspill / perMatch) + 1, 1, considered.Count);

            considered.RemoveRange(considered.Count - drop, drop);
        }
    }

    private JsonObject BuildBody(Fixture fixture, IReadOnlyList<CompletedMatch> relevant) => new()
    {
        ["model"] = settings.Model,
        ["state"] = new JsonObject
        {
            ["fixture"] = new JsonObject
            {
                ["home_team"] = fixture.HomeTeam,
                ["away_team"] = fixture.AwayTeam,
                ["kickoff_utc"] = fixture.KickoffUtc.ToString("u"),
                ["gameweek"] = fixture.Gameweek,
            },
            ["history"] = new JsonArray([.. relevant.Select(ToJson)]),
        },
        ["questions"] = new JsonObject
        {
            [QuestionKey] = new JsonObject
            {
                ["type"] = "choice",
                ["instructions"] = Instructions,
                ["criteria"] = BuildCriteria(),
            },
        },
    };

    private static JsonNode ToJson(CompletedMatch match) => new JsonObject
    {
        ["kickoff"] = match.KickoffUtc.ToString("u"),
        ["home"] = ToJson(match.Home),
        ["away"] = ToJson(match.Away),
    };

    private static JsonNode ToJson(TeamPerformance team)
    {
        var node = new JsonObject
        {
            ["team"] = team.Name,
            ["goals"] = team.Goals,
        };

        if (team.LeaguePositionBefore is { } position) node["position_before"] = position;

        if (team.Stats is { } stats)
        {
            node["shots"] = stats.Shots;
            node["on_target"] = stats.ShotsOnTarget;
            node["off_target"] = stats.ShotsOffTarget;
            node["blocked"] = stats.BlockedShots;
            if (stats.ExpectedGoals is { } xg) node["xg"] = xg;
            if (stats.PossessionPercent is { } possession) node["possession"] = possession;
        }

        return node;
    }

    private static JsonObject BuildCriteria()
    {
        // Option names are self-describing, so descriptions are null per Jev's guidance.
        var criteria = new JsonObject();
        foreach (var scoreline in ScorelineOptions.All)
        {
            criteria[scoreline.Key] = null;
        }

        criteria[ScorelineOptions.OtherKey] = ScorelineOptions.OtherDescription;
        return criteria;
    }

    private static ScorelineProbabilities ReadProbabilities(JsonNode body)
    {
        var answer = body["answers"]?[QuestionKey]
                     ?? throw new JevException($"Jev returned no answer for '{QuestionKey}'.");

        var probabilities = answer["probabilities"]?.AsObject()
                            ?? throw new JevException($"Jev returned no probabilities for '{QuestionKey}'.");

        var byScoreline = new Dictionary<Scoreline, double>();
        var other = 0d;

        foreach (var (key, value) in probabilities)
        {
            var probability = value?.GetValue<double>() ?? 0d;

            if (Scoreline.TryParse(key, out var scoreline))
            {
                byScoreline[scoreline] = probability;
            }
            else
            {
                // "other", and defensively anything else Jev hands back that isn't a scoreline.
                other += probability;
            }
        }

        return new ScorelineProbabilities(
            byScoreline,
            other,
            answer["confidence"]?.GetValue<double>() ?? 0d);
    }
}
