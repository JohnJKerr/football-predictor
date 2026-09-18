namespace Api.IntegrationTests;

using Xunit;

public sealed class WhenCallingTheGameweekEndpoint(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [SkippableFact]
    public async Task A_gameweek_is_predicted_end_to_end()
    {
        // Arrange
        Skip.If(factory.ApiKey is null, "No Jev:ApiKey in user secrets.");
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/gameweeks/5");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_gameweek_outside_the_season_is_not_found()
    {
        // Arrange
        // No Jev call is made, so this runs with or without a key.
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/gameweeks/99");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
