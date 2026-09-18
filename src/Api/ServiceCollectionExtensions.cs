namespace Api;

using Domain.History;
using Domain.Predicting;
using Domain.Schedule;
using External.Data;
using External.Jev;
using Microsoft.Extensions.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFootballPredictor(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JevSettings>(configuration.GetSection(JevSettings.SectionName));
        services.AddSingleton<IJevSettings>(sp => sp.GetRequiredService<IOptions<JevSettings>>().Value);

        var data = new DataFileOptions();
        configuration.GetSection(DataFileOptions.SectionName).Bind(data);

        services.AddSingleton<IFixtureSource>(_ => new JsonFileFixtureSource(data.FixturesPath));
        services.AddSingleton<IMatchHistory>(_ => new JsonFileMatchHistory(data.ResultsPath));
        services.AddSingleton<IRelevantHistory, RelevantHistory>();
        services.AddSingleton<IPriorSeasons>(_ => new JsonFilePriorSeasons(data.PriorSeasonsPath));
        services.AddSingleton<ILeagueContext, LeagueContextSource>();

        services.AddHttpClient<IJevPredictor, JevPredictor>(client =>
            // A gameweek issues ten of these at once and Jev is doing real work on each.
            client.Timeout = TimeSpan.FromMinutes(2));

        services.AddSingleton<IGameweekSchedule, GameweekSchedule>();
        services.AddScoped<IMatchPredictor, MatchPredictor>();
        services.AddScoped<IGameweekPredictor, GameweekPredictor>();

        return services;
    }
}

/// <summary>Where the season datasets live. Defaults to the copies published next to the app.</summary>
public sealed class DataFileOptions
{
    public const string SectionName = "Data";

    public string FixturesPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "fixtures.json");

    public string ResultsPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "results.json");

    public string PriorSeasonsPath { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "data", "prior-seasons.json");
}
