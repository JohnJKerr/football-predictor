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

    /// <summary>Show Jev what it predicted for recent gameweeks and what actually happened.</summary>
    bool IncludeCalibration { get; }
}

/// <summary>
/// TEMPORARY. Bound from the "State" configuration section.
/// <para>
/// Defaults are this season's form only. Running gameweeks 2-4 through every combination
/// scored form alone best on Brier (0.774) and everything-on markedly worse (0.840); the
/// league blocks each made it worse still, base rates worst of all at 1.318. See the
/// experiment note in README.
/// </para>
/// </summary>
public sealed class StateSettings : IStateSettings
{
    public const string SectionName = "State";

    public bool IncludeBaseRates { get; set; }

    public bool IncludeClubRecords { get; set; }

    public bool IncludeHeadToHead { get; set; }

    public bool IncludeRecentForm { get; set; } = true;

    /// <summary>Off until measured, like every other block here.</summary>
    public bool IncludeCalibration { get; set; }

    /// <summary>What the predictor assumes when no settings are supplied.</summary>
    public static IStateSettings Default { get; } = new StateSettings();

    /// <summary>Everything on, for exercising the parts that are no longer sent by default.</summary>
    public static IStateSettings Everything { get; } = new StateSettings
    {
        IncludeBaseRates = true,
        IncludeClubRecords = true,
        IncludeHeadToHead = true,
        IncludeRecentForm = true,
        IncludeCalibration = true,
    };

    /// <summary>Names of the parts in play, so a captured response says how it was produced.</summary>
    public static IReadOnlyList<string> Describe(IStateSettings settings)
    {
        var parts = new List<string>();

        if (settings.IncludeRecentForm) parts.Add("recent_form");
        if (settings.IncludeBaseRates) parts.Add("base_rates");
        if (settings.IncludeClubRecords) parts.Add("club_records");
        if (settings.IncludeHeadToHead) parts.Add("head_to_head");
        if (settings.IncludeCalibration) parts.Add("calibration");

        return parts;
    }
}
