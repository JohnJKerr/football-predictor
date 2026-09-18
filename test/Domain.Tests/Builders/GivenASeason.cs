namespace Domain.Tests.Builders;

using Domain.Schedule;

/// <summary>
/// A synthetic fixture list. Fixtures are an hour apart so kickoff order is unambiguous,
/// and named <c>match-0</c> upwards so assertions can name the ones they expect.
/// </summary>
internal sealed class GivenASeason
{
    private int count;
    private bool reversed;
    private DateTimeOffset? sharedKickoff;

    public static GivenASeason Of(int fixtures) => new() { count = fixtures };

    public GivenASeason InReverseOrder()
    {
        reversed = true;
        return this;
    }

    public GivenASeason AllKickingOffTogether()
    {
        sharedKickoff = new DateTimeOffset(2026, 9, 19, 14, 0, 0, TimeSpan.Zero);
        return this;
    }

    public IReadOnlyList<FixtureListing> Listings()
    {
        var listings = Enumerable.Range(0, count)
            .Select(i => new FixtureListing(
                Id: $"match-{i}",
                KickoffUtc: sharedKickoff
                            ?? new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero).AddHours(i),
                HomeTeam: $"Home {i}",
                AwayTeam: $"Away {i}"))
            .ToList();

        if (reversed) listings.Reverse();

        return listings;
    }

    public IFixtureSource Build() => new StubFixtureSource(Listings());

    private sealed class StubFixtureSource(IReadOnlyList<FixtureListing> fixtures) : IFixtureSource
    {
        public Task<IReadOnlyList<FixtureListing>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(fixtures);
    }
}
