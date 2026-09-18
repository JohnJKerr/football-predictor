namespace Domain.Schedule;

/// <summary>The season's published fixture list. Implemented in External.</summary>
public interface IFixtureSource
{
    Task<IReadOnlyList<FixtureListing>> GetAllAsync(CancellationToken cancellationToken = default);
}
