namespace Domain.Tests.Builders;

using Domain.History;

internal sealed class GivenAHistory
{
    private CompletedMatch[] matches = [];
    private int? maxPerClub;

    public static GivenAHistory Of(params CompletedMatch[] matches) => new() { matches = matches };

    public static GivenAHistory OfNothing() => new();

    public GivenAHistory KeepingAtMostPerClub(int limit)
    {
        maxPerClub = limit;
        return this;
    }

    public RelevantHistory Build() => maxPerClub is { } limit
        ? new RelevantHistory(new StubMatchHistory(matches), limit)
        : new RelevantHistory(new StubMatchHistory(matches));

    private sealed class StubMatchHistory(CompletedMatch[] matches) : IMatchHistory
    {
        public Task<IReadOnlyList<CompletedMatch>> GetCompletedAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompletedMatch>>(matches);
    }
}
