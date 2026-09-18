namespace External.Tests.Jev;

using Domain.History;
using Domain.Model;

internal sealed class StubRelevantHistory(params CompletedMatch[] matches) : IRelevantHistory
{
    public Fixture? Asked { get; private set; }

    public Task<IReadOnlyList<CompletedMatch>> ForAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        Asked = fixture;
        return Task.FromResult<IReadOnlyList<CompletedMatch>>(matches);
    }

    public static CompletedMatch Match(int day, string home, string away, int homeGoals = 2, int awayGoals = 1) =>
        new(new DateTimeOffset(2026, 8, day, 14, 0, 0, TimeSpan.Zero),
            new TeamPerformance(home, homeGoals, 5, new MatchStats(14, 5, 6, 3, 1.62, 55.4)),
            new TeamPerformance(away, awayGoals, 12, new MatchStats(9, 3, 4, 2, 0.91, 44.6)));
}
