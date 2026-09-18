namespace External.Tests.Builders;

using External.Data;

/// <summary>Locates the season datasets that ship alongside the solution and opens them.</summary>
internal sealed class GivenAFile
{
    private string? path;

    public static GivenAFile WithFixtures() => new() { path = Path.Combine(Root, "fixtures.json") };

    public static GivenAFile WithResults() => new() { path = Path.Combine(Root, "results.json") };

    public JsonFileFixtureSource BuildFixtureSource() => new(path!);

    public JsonFileMatchHistory BuildMatchHistory() => new(path!);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                var data = Path.Combine(directory.FullName, "data");
                if (Directory.Exists(data)) return data;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the solution's data directory.");
        }
    }
}
