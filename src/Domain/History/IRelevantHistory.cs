namespace Domain.History;

using Domain.Model;

/// <summary>Chooses the slice of the season worth sending to Jev for a given fixture.</summary>
public interface IRelevantHistory
{
    Task<IReadOnlyList<CompletedMatch>> ForAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
