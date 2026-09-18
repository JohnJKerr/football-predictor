namespace Domain.History;

using Domain.Model;

/// <summary>
/// What completed seasons say about this fixture: how the league behaves in aggregate, how
/// each club fares at the venue it is about to play at, and how the two have met before.
/// A club promoted into the league has no record, and none is invented.
/// </summary>
public sealed record LeagueContext(
    LeagueBaseRates BaseRates,
    ClubRecord? HomeClubAtHome,
    ClubRecord? AwayClubAwayFromHome,
    HeadToHead? PreviousMeetings);

public interface ILeagueContext
{
    Task<LeagueContext> ForAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
