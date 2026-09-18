namespace Domain.History;

/// <summary>A club's record over completed seasons, at one venue.</summary>
public sealed record ClubRecord(
    string Club,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst);

/// <summary>
/// Previous meetings between the two clubs, at either venue, recorded from the point of
/// view of whichever club is at home in the fixture being predicted.
/// </summary>
public sealed record HeadToHead(int Played, int Won, int Drawn, int Lost);
