namespace External.Tests.Data;

/// <summary>Locates the season datasets that ship alongside the solution.</summary>
internal static class DataFiles
{
    public static string Fixtures => Path.Combine(Root, "fixtures.json");

    public static string Results => Path.Combine(Root, "results.json");

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
