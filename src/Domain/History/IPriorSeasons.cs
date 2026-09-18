namespace Domain.History;

/// <summary>Completed seasons before the one being predicted. Implemented in External.</summary>
public interface IPriorSeasons
{
    Task<IReadOnlyList<PriorResult>> GetAsync(CancellationToken cancellationToken = default);
}
