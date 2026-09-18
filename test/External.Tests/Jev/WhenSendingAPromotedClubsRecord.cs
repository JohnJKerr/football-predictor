namespace External.Tests.Jev;

using Domain.History;
using External.Tests.Builders;

public class WhenSendingAPromotedClubsRecord
{
    private static ClubRecord Borrowed(string club) =>
        new(club, 114, 26, 28, 60, 114, 195, RecordBasis.PromotedSides);

    [Fact]
    public async Task A_borrowed_record_says_where_its_figures_came_from()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(homeAtHome: Borrowed("Coventry City"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("promoted_sides", (string?)jev.League["home_club_at_home"]!["basis"]);
    }

    [Fact]
    public async Task A_borrowed_record_explains_itself_in_words()
    {
        // Arrange
        // The figures are not this club's own, and Jev must not read them as if they were.
        var jev = GivenJev.WithLeague(
            ALeague.Of(homeAtHome: Borrowed("Coventry City"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Contains(
            "no record",
            (string?)jev.League["home_club_at_home"]!["note"] ?? string.Empty);
    }

    [Fact]
    public async Task A_borrowed_record_names_the_club_it_stands_for()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(homeAtHome: Borrowed("Coventry City"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Contains("Coventry City", (string?)jev.League["home_club_at_home"]!["note"] ?? string.Empty);
    }

    [Fact]
    public async Task A_clubs_own_record_says_so()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(homeAtHome: ALeague.Record("Nottingham Forest"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Equal("own_record", (string?)jev.League["home_club_at_home"]!["basis"]);
    }

    [Fact]
    public async Task A_clubs_own_record_needs_no_explanation()
    {
        // Arrange
        var jev = GivenJev.WithLeague(
            ALeague.Of(homeAtHome: ALeague.Record("Nottingham Forest"))).Build();

        // Act
        await jev.PredictAsync(AFixture.Upcoming);

        // Assert
        Assert.Null(jev.League["home_club_at_home"]!["note"]);
    }
}
