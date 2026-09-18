namespace External.Data;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.History;

/// <summary>
/// Reads completed seasons from the on-disk results dataset. Three seasons are 120 KB, far
/// past what Jev will accept, so these exist to be summarised rather than sent.
/// </summary>
public sealed class JsonFilePriorSeasons(string path) : IPriorSeasons
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IReadOnlyList<PriorResult>? cached;

    public async Task<IReadOnlyList<PriorResult>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cached is not null) return cached;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cached is not null) return cached;

            await using var stream = File.OpenRead(path);
            var document = await JsonSerializer.DeserializeAsync<SeasonsFile>(stream, Options, cancellationToken)
                           ?? throw new InvalidDataException($"'{path}' is not a results file.");

            return cached =
            [
                .. document.Results.Select(r => new PriorResult(
                    DateOnly.ParseExact(r.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    r.HomeTeam,
                    r.HomeScore,
                    r.AwayTeam,
                    r.AwayScore))
            ];
        }
        finally
        {
            gate.Release();
        }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private sealed record SeasonsFile(
        [property: JsonPropertyName("results")] IReadOnlyList<ResultRecord> Results);

    private sealed record ResultRecord(
        string Date, string HomeTeam, int HomeScore, string AwayTeam, int AwayScore);
}
