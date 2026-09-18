namespace Domain.History;

/// <summary>Where a record came from: the club's own matches, or the class it belongs to.</summary>
public enum RecordBasis
{
    OwnRecord,
    PromotedSides,
}

/// <summary>A club's record over completed seasons, at one venue.</summary>
public sealed record ClubRecord(
    string Club,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    RecordBasis Basis = RecordBasis.OwnRecord);

/// <summary>
/// Previous meetings between the two clubs, at either venue, recorded from the point of
/// view of whichever club is at home in the fixture being predicted.
/// </summary>
public sealed record HeadToHead(int Played, int Won, int Drawn, int Lost);
