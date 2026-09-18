namespace External.Tests.Jev;

using External.Jev;
using static External.Tests.Jev.JevScenario;

public class WhenBuildingTheStateSentToJev
{
    [Fact]
    public async Task The_fixture_under_question_is_described()
    {
        var (predictor, handler) = Build(Match(8, "Hull City", "Coventry City"));

        await predictor.PredictAsync(Upcoming);

        var fixture = handler.Body["state"]!["fixture"]!;
        Assert.Equal("Nottingham Forest", (string?)fixture["home_team"]);
        Assert.Equal("Coventry City", (string?)fixture["away_team"]);
        Assert.Equal(5, (int?)fixture["gameweek"]);
    }

    [Fact]
    public async Task Only_the_history_judged_relevant_to_this_fixture_is_sent()
    {
        var (predictor, handler) = Build(
            Match(1, "Nottingham Forest", "Everton", 2, 0),
            Match(8, "Hull City", "Coventry City", 1, 1));

        await predictor.PredictAsync(Upcoming);

        var history = handler.Body["state"]!["history"]!.AsArray();

        // Most recent first: the 8th precedes the 1st.
        Assert.Equal(2, history.Count);
        Assert.Equal("Hull City", (string?)history[0]!["home"]!["team"]);
        Assert.Equal("Nottingham Forest", (string?)history[1]!["home"]!["team"]);
        Assert.Equal(2, (int?)history[1]!["home"]!["goals"]);
    }

    [Fact]
    public async Task The_shooting_and_possession_figures_are_carried_through()
    {
        var (predictor, handler) = Build(Match(1, "Nottingham Forest", "Everton"));

        await predictor.PredictAsync(Upcoming);

        var home = handler.Body["state"]!["history"]![0]!["home"]!;

        Assert.Equal(14, (int?)home["shots"]);
        Assert.Equal(1.62, (double?)home["xg"]);
        Assert.Equal(55.4, (double?)home["possession"]);
    }

    [Fact]
    public async Task The_history_is_asked_about_the_fixture_under_question()
    {
        var handler = new RecordingHandler(AnyValidReply);
        var history = new StubRelevantHistory(Match(1, "Nottingham Forest", "Everton"));
        var predictor = new JevPredictor(new HttpClient(handler), new TestJevSettings(), history);

        await predictor.PredictAsync(Upcoming);

        Assert.Same(Upcoming, history.Asked);
    }

    [Fact]
    public async Task A_fixture_between_two_clubs_with_no_history_is_still_asked_about()
    {
        var (predictor, handler) = Build();

        await predictor.PredictAsync(Upcoming);

        Assert.Empty(handler.Body["state"]!["history"]!.AsArray());
        Assert.NotNull(handler.Body["questions"]!["scoreline"]);
    }

    [Fact]
    public async Task Jevs_context_limit_is_respected_by_dropping_the_oldest_matches()
    {
        // Far more history than Jev will accept in one request.
        var crowded = Enumerable.Range(1, 400)
            .Select(i => Match(1 + (i % 28), $"Club {i}", "Coventry City"))
            .ToArray();

        var (predictor, handler) = Build(crowded);

        await predictor.PredictAsync(Upcoming);

        var bytes = handler.Request!.Content!.Headers.ContentLength!.Value;

        Assert.True(bytes <= JevPredictor.MaxRequestBytes,
            $"Request was {bytes} bytes, over the {JevPredictor.MaxRequestBytes} byte limit.");
        Assert.NotEmpty(handler.Body["state"]!["history"]!.AsArray());
    }

    [Fact]
    public async Task The_most_recent_form_survives_when_history_has_to_be_dropped()
    {
        var crowded = Enumerable.Range(1, 400)
            .Select(i => Match(1 + (i % 28), $"Club {i}", "Coventry City"))
            .ToArray();

        var (predictor, handler) = Build(crowded);

        await predictor.PredictAsync(Upcoming);

        var kept = handler.Body["state"]!["history"]!.AsArray();
        var oldestKept = kept.Min(m => DateTimeOffset.Parse((string)m!["kickoff"]!));

        Assert.True(crowded.Length > kept.Count, "Expected this much history to be trimmed.");
        Assert.True(crowded.Count(m => m.KickoffUtc > oldestKept) <= kept.Count);
    }
}
