namespace Domain.Tests.Predicting;

using Domain.Model;
using Domain.Predicting;

public class WhenPredictingAMatch
{
    private static readonly Fixture Fixture = new(
        Id: "espn:401879270",
        Gameweek: 5,
        KickoffUtc: new DateTimeOffset(2026, 9, 19, 16, 30, 0, TimeSpan.Zero),
        HomeTeam: "Nottingham Forest",
        AwayTeam: "Coventry City");

    private static ScorelineProbabilities Distribution(
        double other = 0d, params (int Home, int Away, double Probability)[] scorelines) =>
        new(scorelines.ToDictionary(s => new Scoreline(s.Home, s.Away), s => s.Probability),
            other,
            Confidence: 0.8);

    [Fact]
    public async Task Candidate_scorelines_are_ranked_most_likely_first()
    {
        var jev = new StubJevPredictor(Distribution(
            other: 0d,
            (1, 0, 0.20),
            (2, 1, 0.35),
            (0, 0, 0.45)));

        var predictions = await new MatchPredictor(jev).PredictAsync(Fixture);

        Assert.Equal(
            [(0, 0), (2, 1), (1, 0)],
            predictions.Select(p => (p.HomeScore, p.AwayScore)));
    }

    [Fact]
    public async Task Each_scoreline_carries_Jevs_own_probability_as_its_confidence()
    {
        var jev = new StubJevPredictor(Distribution(other: 0d, (2, 1, 0.35), (1, 0, 0.20)));

        var predictions = await new MatchPredictor(jev).PredictAsync(Fixture);

        Assert.Equal(0.35, predictions[0].Confidence);
        Assert.Equal(0.20, predictions[1].Confidence);
    }

    [Fact]
    public async Task Scorelines_Jev_gave_no_chance_of_happening_are_omitted()
    {
        var jev = new StubJevPredictor(Distribution(
            other: 0d,
            (1, 0, 0.60),
            (4, 4, 0.0),
            (2, 1, 0.40)));

        var predictions = await new MatchPredictor(jev).PredictAsync(Fixture);

        Assert.Equal(2, predictions.Count);
        Assert.DoesNotContain(predictions, p => p is { HomeScore: 4, AwayScore: 4 });
    }

    [Fact]
    public async Task The_catch_all_bucket_is_excluded_because_it_is_not_a_scoreline()
    {
        // "other" holds the most mass, but it names no result we could predict.
        var jev = new StubJevPredictor(Distribution(other: 0.70, (1, 0, 0.30)));

        var predictions = await new MatchPredictor(jev).PredictAsync(Fixture);

        var only = Assert.Single(predictions);
        Assert.Equal(new Prediction(1, 0, 0.30), only);
    }

    [Fact]
    public async Task Jev_is_asked_about_the_fixture_it_was_given()
    {
        var jev = new StubJevPredictor(Distribution(other: 0d, (1, 0, 1.0)));

        await new MatchPredictor(jev).PredictAsync(Fixture);

        Assert.Same(Fixture, jev.Asked);
    }
}
