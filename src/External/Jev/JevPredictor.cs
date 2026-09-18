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
public sealed class JevPredictor(
    HttpClient httpClient,
    IJevSettings settings,
    IRelevantHistory history,
    ILeagueContext league,
    IStateSettings? state = null)
    : IJevPredictor
{
    // TEMPORARY: null means everything on, which is what every caller but the experiment wants.
    private readonly IStateSettings state = state ?? StateSettings.All;

    private const string ScorelineKey = "scoreline";
    private const string OutcomeKey = "outcome";
    private const string OverGoalsKey = "over_two_and_a_half_goals";
    private const string BothScoreKey = "both_teams_to_score";
    private const string Endpoint = "v1/systemone";

    /// <summary>Jev accepts at most 32 KB of context per request.</summary>
    public const int MaxRequestBytes = 32 * 1024;

    private const string ScorelineQuestion =
        "What is the likely full-time score? Options are written as home goals to away goals, " +
        "so \"2-1\" means the home team wins by two goals to one.";

    private const string OutcomeQuestion = "Which way is this match likely to go?";

    private const string OverGoalsQuestion = "Will this match finish with three or more goals in total?";

    private const string BothScoreQuestion = "Will both clubs score at least once?";

    /// <summary>
    /// Names only the parts of the state actually sent. Pointing Jev at a block that is not
    /// there would have the experiment measuring confusion rather than the block's absence.
    /// </summary>
    private string Preamble
    {
        get
        {
            var sources = new List<string> { "the fixture in `fixture`" };

            if (state.IncludeRecentForm) sources.Insert(0, "the recent results in `history`");
            if (state.IncludeBaseRates || state.IncludeClubRecords || state.IncludeHeadToHead)
            {
                sources.Add("the long-run record in `league`");
            }

            var preamble = $"This is an upcoming Premier League fixture. Use {string.Join(", ", sources)}. ";

            if (state.IncludeBaseRates)
            {
                preamble +=
                    "The league's own rates over the last three seasons are in " +
                    "`league.base_rates`; unless this fixture gives reason to differ, your " +
                    "answer should be consistent with them. ";
            }

            return preamble;
        }
    }

    public async Task<MatchForecast> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        var relevant = await history.ForAsync(fixture, cancellationToken);
        var context = await league.ForAsync(fixture, cancellationToken);
        var payload = BuildPayloadWithinBudget(fixture, relevant, context);

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

        return ReadForecast(body);
    }

    /// <summary>
    /// Serialises the request, shedding the oldest matches until it fits Jev's context limit.
    /// <see cref="RelevantHistory"/> has already narrowed this to the two clubs; this is the
    /// transport's own backstop, so a long season cannot silently produce a rejected request.
    /// </summary>
    private byte[] BuildPayloadWithinBudget(
        Fixture fixture, IReadOnlyList<CompletedMatch> relevant, LeagueContext context)
    {
        // Most recent first, so trimming from the end sheds the least useful history.
        var considered = relevant.OrderByDescending(m => m.KickoffUtc).ToList();

        while (true)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(BuildBody(fixture, considered, context));

            if (payload.Length <= MaxRequestBytes || considered.Count == 0) return payload;

            // Drop roughly the overspill in one go rather than one match at a time.
            var overspill = payload.Length - MaxRequestBytes;
            var perMatch = Math.Max(1, (payload.Length / Math.Max(1, considered.Count)));
            var drop = Math.Clamp((overspill / perMatch) + 1, 1, considered.Count);

            considered.RemoveRange(considered.Count - drop, drop);
        }
    }

    private JsonObject BuildBody(
        Fixture fixture, IReadOnlyList<CompletedMatch> relevant, LeagueContext context) => new()
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
            ["history"] = new JsonArray([.. (state.IncludeRecentForm ? relevant : []).Select(ToJson)]),
            // Summarised rather than sent: three completed seasons are 120 KB of results.
            ["league"] = ToJson(context),
        },
        // Questions run in parallel inside one request, so the narrower ones cost no
        // extra round trip and share the state we already paid to send.
        ["questions"] = new JsonObject
        {
            [ScorelineKey] = new JsonObject
            {
                ["type"] = "choice",
                ["instructions"] = Preamble + ScorelineQuestion,
                ["criteria"] = BuildScorelineCriteria(),
            },
            [OutcomeKey] = new JsonObject
            {
                ["type"] = "choice",
                ["instructions"] = Preamble + OutcomeQuestion,
                ["criteria"] = BuildOutcomeCriteria(fixture),
            },
            [OverGoalsKey] = Noul(
                Preamble + OverGoalsQuestion,
                whenTrue: "Three or more goals are scored in total.",
                whenFalse: "Two or fewer goals are scored in total."),
            [BothScoreKey] = Noul(
                Preamble + BothScoreQuestion,
                whenTrue: "Both clubs score at least one goal.",
                whenFalse: "At least one club fails to score."),
        },
    };

    private static JsonObject Noul(string instructions, string whenTrue, string whenFalse) => new()
    {
        ["type"] = "noul",
        ["instructions"] = instructions,
        ["criteria"] = new JsonObject { ["true"] = whenTrue, ["false"] = whenFalse },
    };

    private static JsonObject BuildOutcomeCriteria(Fixture fixture) => new()
    {
        // Named for the clubs: "home_win" alone does not say who is at home.
        ["home_win"] = $"{fixture.HomeTeam} win at home against {fixture.AwayTeam}.",
        ["draw"] = $"{fixture.HomeTeam} and {fixture.AwayTeam} finish level.",
        ["away_win"] = $"{fixture.AwayTeam} win away at {fixture.HomeTeam}.",
    };

    private JsonNode ToJson(LeagueContext context)
    {
        var node = new JsonObject();

        if (state.IncludeBaseRates)
        {
            node["base_rates"] = new JsonObject
            {
                ["matches"] = context.BaseRates.Matches,
                ["home_win"] = context.BaseRates.HomeWin,
                ["draw"] = context.BaseRates.Draw,
                ["away_win"] = context.BaseRates.AwayWin,
                ["goals_per_match"] = context.BaseRates.GoalsPerMatch,
                ["over_two_and_a_half_goals"] = context.BaseRates.OverTwoAndAHalfGoals,
                ["both_teams_to_score"] = context.BaseRates.BothTeamsToScore,
            };
        }

        if (state.IncludeClubRecords)
        {
            // A club promoted into the league has no record; omit rather than send zeroes,
            // which would read as a club that played and never won.
            if (context.HomeClubAtHome is { } home) node["home_club_at_home"] = ToJson(home);
            if (context.AwayClubAwayFromHome is { } away) node["away_club_away_from_home"] = ToJson(away);
        }

        if (state.IncludeHeadToHead && context.PreviousMeetings is { } met)
        {
            node["previous_meetings"] = new JsonObject
            {
                ["played"] = met.Played,
                ["won"] = met.Won,
                ["drawn"] = met.Drawn,
                ["lost"] = met.Lost,
            };
        }

        return node;
    }

    private static JsonNode ToJson(ClubRecord record)
    {
        var node = new JsonObject
        {
            ["club"] = record.Club,
            ["basis"] = record.Basis == RecordBasis.OwnRecord ? "own_record" : "promoted_sides",
            ["played"] = record.Played,
            ["won"] = record.Won,
            ["drawn"] = record.Drawn,
            ["lost"] = record.Lost,
            ["goals_for"] = record.GoalsFor,
            ["goals_against"] = record.GoalsAgainst,
        };

        if (record.Basis == RecordBasis.PromotedSides)
        {
            // Said in words as well as in the field: these are not this club's own figures,
            // and read as if they were they would badly overstate its experience.
            node["note"] =
                $"{record.Club} have no record in these seasons, having just come up. These " +
                "are the combined figures for clubs in the season they were promoted.";
        }

        return node;
    }

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

    private static JsonObject BuildScorelineCriteria()
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

    private static MatchForecast ReadForecast(JsonNode body)
    {
        var answers = body["answers"];

        return new MatchForecast(
            ReadScorelines(answers),
            ReadOutcome(answers),
            // A narrower question going missing must not cost us the scoreline, which is
            // the answer the endpoint was built around.
            ReadNoul(answers, OverGoalsKey),
            ReadNoul(answers, BothScoreKey));
    }

    private static double ReadNoul(JsonNode? answers, string key) =>
        answers?[key]?["noul"]?.GetValue<double>() ?? 0d;

    private static OutcomeProbabilities ReadOutcome(JsonNode? answers)
    {
        var answer = answers?[OutcomeKey];
        var probabilities = answer?["probabilities"]?.AsObject();

        if (probabilities is null) return new OutcomeProbabilities(new Dictionary<Outcome, double>(), 0d);

        var byOutcome = new Dictionary<Outcome, double>();

        foreach (var (key, value) in probabilities)
        {
            if (OutcomeKeys.TryGetValue(key, out var outcome))
            {
                byOutcome[outcome] = value?.GetValue<double>() ?? 0d;
            }
        }

        return new OutcomeProbabilities(byOutcome, answer?["confidence"]?.GetValue<double>() ?? 0d);
    }

    private static readonly Dictionary<string, Outcome> OutcomeKeys = new()
    {
        ["home_win"] = Outcome.HomeWin,
        ["draw"] = Outcome.Draw,
        ["away_win"] = Outcome.AwayWin,
    };

    private static ScorelineProbabilities ReadScorelines(JsonNode? answers)
    {
        var answer = answers?[ScorelineKey]
                     ?? throw new JevException($"Jev returned no answer for '{ScorelineKey}'.");

        var probabilities = answer["probabilities"]?.AsObject()
                            ?? throw new JevException($"Jev returned no probabilities for '{ScorelineKey}'.");

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
