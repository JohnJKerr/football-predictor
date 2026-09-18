namespace Domain.Model;

/// <summary>A specific full-time result, e.g. 2-1.</summary>
public readonly record struct Scoreline(int HomeScore, int AwayScore)
{
    /// <summary>The wire key Jev uses to name this scoreline as a choice option.</summary>
    public string Key => $"{HomeScore}-{AwayScore}";

    public static bool TryParse(string? key, out Scoreline scoreline)
    {
        scoreline = default;
        if (key is null) return false;

        var separator = key.IndexOf('-');
        if (separator <= 0 || separator == key.Length - 1) return false;

        if (!int.TryParse(key.AsSpan(0, separator), out var home)) return false;
        if (!int.TryParse(key.AsSpan(separator + 1), out var away)) return false;
        if (home < 0 || away < 0) return false;

        scoreline = new Scoreline(home, away);
        return true;
    }

    public override string ToString() => Key;
}
