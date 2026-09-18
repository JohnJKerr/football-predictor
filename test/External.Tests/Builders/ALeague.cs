namespace External.Tests.Builders;

using Domain.History;

internal static class ALeague
{
    public static LeagueContext Of(
        ClubRecord? homeAtHome = null,
        ClubRecord? awayFromHome = null,
        HeadToHead? meetings = null) =>
        new(
            new LeagueBaseRates(1140, 0.432, 0.245, 0.324, 2.988, 0.588, 0.583),
            homeAtHome,
            awayFromHome,
            meetings);

    public static ClubRecord Record(string club) => new(club, 57, 18, 15, 24, 73, 82);
}
