namespace External.Jev;

/// <summary>
/// TEMPORARY. Switches individual parts of the state sent to Jev on and off, so a gameweek
/// can be run with and without each and the results compared.
/// <para>
/// Added because the league block landed as one change — base rates, club records and
/// head-to-head together — and the result could not be attributed to any of them. Delete
/// this once the question is settled.
/// </para>
/// </summary>
public interface IStateSettings
{
    bool IncludeBaseRates { get; }

    bool IncludeClubRecords { get; }

    bool IncludeHeadToHead { get; }

    bool IncludeRecentForm { get; }
}

/// <summary>TEMPORARY. Bound from the "State" configuration section; everything on by default.</summary>
public sealed class StateSettings : IStateSettings
{
    public const string SectionName = "State";

    public bool IncludeBaseRates { get; set; } = true;

    public bool IncludeClubRecords { get; set; } = true;

    public bool IncludeHeadToHead { get; set; } = true;

    public bool IncludeRecentForm { get; set; } = true;

    /// <summary>Everything on: what the predictor assumes when no settings are supplied.</summary>
    public static IStateSettings All { get; } = new StateSettings();

    /// <summary>Names of the parts in play, so a captured response says how it was produced.</summary>
    public static IReadOnlyList<string> Describe(IStateSettings settings)
    {
        var parts = new List<string>();

        if (settings.IncludeRecentForm) parts.Add("recent_form");
        if (settings.IncludeBaseRates) parts.Add("base_rates");
        if (settings.IncludeClubRecords) parts.Add("club_records");
        if (settings.IncludeHeadToHead) parts.Add("head_to_head");

        return parts;
    }
}
