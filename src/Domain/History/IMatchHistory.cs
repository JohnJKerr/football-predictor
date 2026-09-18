namespace Domain.History;

/// <summary>Every completed match of the season. Implemented in External.</summary>
public interface IMatchHistory
{
    Task<IReadOnlyList<CompletedMatch>> GetCompletedAsync(CancellationToken cancellationToken = default);
}
