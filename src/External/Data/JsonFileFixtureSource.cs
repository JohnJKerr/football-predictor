namespace External.Data;

using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Schedule;

/// <summary>Reads the published season fixture list from the on-disk dataset.</summary>
public sealed class JsonFileFixtureSource(string path) : IFixtureSource
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IReadOnlyList<FixtureListing>? cached;

    public async Task<IReadOnlyList<FixtureListing>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (cached is not null) return cached;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cached is not null) return cached;

            await using var stream = File.OpenRead(path);
            var document = await JsonSerializer.DeserializeAsync<FixtureFile>(stream, Options, cancellationToken)
                           ?? throw new InvalidDataException($"'{path}' is not a fixture list.");

            return cached =
            [
                .. document.Fixtures.Select(f => new FixtureListing(
                    f.Id,
                    DateTimeOffset.Parse(f.KickoffUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
                    f.HomeTeam,
                    f.AwayTeam))
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

    private sealed record FixtureFile(
        [property: JsonPropertyName("fixtures")] IReadOnlyList<FixtureRecord> Fixtures);

    private sealed record FixtureRecord(string Id, string KickoffUtc, string HomeTeam, string AwayTeam);
}
