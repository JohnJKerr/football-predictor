namespace Domain.Tests.History;

using Domain.History;
using Domain.Model;

public class WhenSelectingRelevantHistory
{
    private static readonly Fixture Fixture = new(
        "espn:401879270", 5,
        new DateTimeOffset(2026, 9, 19, 16, 30, 0, TimeSpan.Zero),
        "Nottingham Forest", "Coventry City");

    private static CompletedMatch Match(int day, string home, string away, int homeGoals = 1, int awayGoals = 0) =>
        new(new DateTimeOffset(2026, 8, day, 14, 0, 0, TimeSpan.Zero),
            new TeamPerformance(home, homeGoals, null, null),
            new TeamPerformance(away, awayGoals, null, null));

    private sealed class Season(params CompletedMatch[] matches) : IMatchHistory
    {
        public Task<IReadOnlyList<CompletedMatch>> GetCompletedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompletedMatch>>(matches);
    }

    private static RelevantHistory Of(params CompletedMatch[] matches) => new(new Season(matches));

    [Fact]
    public async Task Only_matches_involving_one_of_the_two_clubs_are_kept()
    {
        var history = Of(
            Match(1, "Nottingham Forest", "Everton"),
            Match(2, "Arsenal", "Liverpool"),
            Match(3, "Hull City", "Coventry City"));

        var relevant = await history.ForAsync(Fixture);

        Assert.Equal(2, relevant.Count);
        Assert.DoesNotContain(relevant, m => m.Involves("Arsenal"));
    }

    [Fact]
    public async Task A_club_is_found_whether_it_played_at_home_or_away()
    {
        var history = Of(
            Match(1, "Nottingham Forest", "Everton"),
            Match(2, "Everton", "Nottingham Forest"));

        Assert.Equal(2, (await history.ForAsync(Fixture)).Count);
    }

    [Fact]
    public async Task The_most_recent_form_comes_first()
    {
        var history = Of(
            Match(1, "Nottingham Forest", "Everton"),
            Match(20, "Coventry City", "Arsenal"),
            Match(10, "Hull City", "Nottingham Forest"));

        var relevant = await history.ForAsync(Fixture);

        Assert.Equal([20, 10, 1], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task A_previous_meeting_between_the_two_clubs_appears_once()
    {
        var history = Of(Match(1, "Coventry City", "Nottingham Forest"));

        Assert.Single(await history.ForAsync(Fixture));
    }

    [Fact]
    public async Task No_more_than_the_configured_number_of_matches_per_club_is_kept()
    {
        // Six matches for one club, none for the other.
        var history = new RelevantHistory(
            new Season([.. Enumerable.Range(1, 6).Select(d => Match(d, "Nottingham Forest", $"Club {d}"))]),
            maxMatchesPerClub: 4);

        var relevant = await history.ForAsync(Fixture);

        Assert.Equal(4, relevant.Count);
        // The four most recent survive.
        Assert.Equal([6, 5, 4, 3], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task The_fixture_being_predicted_is_never_part_of_its_own_history()
    {
        // The dataset holds completed matches, so a fixture already played appears in it.
        // Handing it back would let Jev read the answer straight off the state.
        var history = Of(new CompletedMatch(
            Fixture.KickoffUtc,
            new TeamPerformance("Nottingham Forest", 2, null, null),
            new TeamPerformance("Coventry City", 1, null, null)));

        Assert.Empty(await history.ForAsync(Fixture));
    }

    [Fact]
    public async Task Matches_played_after_the_fixture_kicks_off_are_excluded()
    {
        var history = Of(
            Match(1, "Nottingham Forest", "Everton"),
            // Later in the season than the fixture under question.
            new CompletedMatch(
                Fixture.KickoffUtc.AddDays(7),
                new TeamPerformance("Coventry City", 3, null, null),
                new TeamPerformance("Arsenal", 0, null, null)));

        var relevant = await history.ForAsync(Fixture);

        Assert.Single(relevant);
        Assert.True(relevant[0].KickoffUtc < Fixture.KickoffUtc);
    }

    [Fact]
    public async Task Form_is_counted_from_the_matches_that_preceded_the_fixture()
    {
        // Eight earlier matches, capped at four per club, must not be filled out with later ones.
        var earlier = Enumerable.Range(1, 8)
            .Select(d => Match(d, "Nottingham Forest", $"Club {d}"));
        var later = new CompletedMatch(
            Fixture.KickoffUtc.AddDays(3),
            new TeamPerformance("Nottingham Forest", 9, null, null),
            new TeamPerformance("Club Z", 0, null, null));

        var history = new RelevantHistory(new Season([.. earlier, later]), maxMatchesPerClub: 4);

        var relevant = await history.ForAsync(Fixture);

        Assert.Equal(4, relevant.Count);
        Assert.All(relevant, m => Assert.True(m.KickoffUtc < Fixture.KickoffUtc));
        Assert.Equal([8, 7, 6, 5], relevant.Select(m => m.KickoffUtc.Day));
    }

    [Fact]
    public async Task A_club_with_no_matches_yet_simply_contributes_nothing()
    {
        var history = Of(Match(1, "Nottingham Forest", "Everton"));

        var relevant = await history.ForAsync(Fixture);

        Assert.Single(relevant);
    }
}
