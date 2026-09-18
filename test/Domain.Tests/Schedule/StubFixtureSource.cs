namespace Domain.Tests.Schedule;

using Domain.Schedule;

internal sealed class StubFixtureSource(IReadOnlyList<FixtureListing> fixtures) : IFixtureSource
{
    public Task<IReadOnlyList<FixtureListing>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(fixtures);
}
