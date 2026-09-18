namespace External.Tests.Jev;

using External.Tests.Builders;

/// <summary>
/// An exact scoreline is irreducibly uncertain — the best any one of fifty options can hold is
/// a fifth of the probability. These narrower questions are asked in the same request, where
/// three or two options concentrate enough probability to be worth acting on.
/// </summary>
public class WhenAskingJevAtomicQuestions
{
    [Fact]
    public async Task All_the_questions_travel_in_a_single_request()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(
            ["scoreline", "outcome", "over_two_and_a_half_goals", "both_teams_to_score"],
            jev.Questions.Select(q => q.Key));
    }

    [Fact]
    public async Task The_outcome_is_asked_as_a_choice()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("choice", (string?)jev.QuestionNamed("outcome")["type"]);
    }

    [Fact]
    public async Task The_outcome_offers_a_home_win_a_draw_and_an_away_win()
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal(
            ["home_win", "draw", "away_win"],
            jev.QuestionNamed("outcome")["criteria"]!.AsObject().Select(o => o.Key));
    }

    [Fact]
    public async Task Each_outcome_option_is_described_in_terms_of_the_two_clubs()
    {
        // Arrange
        // "home_win" means nothing without saying which club is at home.
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Contains(
            "Nottingham Forest",
            (string?)jev.QuestionNamed("outcome")["criteria"]!["home_win"]);
    }

    [Theory]
    [InlineData("over_two_and_a_half_goals")]
    [InlineData("both_teams_to_score")]
    public async Task A_yes_or_no_question_is_asked_as_a_noul(string question)
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("noul", (string?)jev.QuestionNamed(question)["type"]);
    }

    [Theory]
    [InlineData("outcome")]
    [InlineData("over_two_and_a_half_goals")]
    [InlineData("both_teams_to_score")]
    public async Task Every_question_carries_instructions(string question)
    {
        // Arrange
        var jev = GivenJev.Asked().Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace((string?)jev.QuestionNamed(question)["instructions"]));
    }
}
