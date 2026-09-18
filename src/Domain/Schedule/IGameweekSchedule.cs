namespace Domain.Schedule;

using Domain.Model;

public interface IGameweekSchedule
{
    Task<IReadOnlyList<Fixture>> GetGameweekAsync(int gameweek, CancellationToken cancellationToken = default);
}
