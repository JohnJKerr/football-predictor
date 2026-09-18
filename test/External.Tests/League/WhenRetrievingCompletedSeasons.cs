namespace External.Tests.League;

using Domain.History;
using External.Data;
using External.Tests.Builders;

public class WhenRetrievingCompletedSeasons
{
    private static JsonFilePriorSeasons Seasons() => GivenAFile.WithPriorSeasons().BuildPriorSeasons();

    [Fact]
    public async Task Every_result_of_the_completed_seasons_is_read()
    {
        // Arrange
        var seasons = Seasons();

        // Act
        var results = await seasons.GetAsync();

        // Assert
        // Three seasons of 380 matches.
        Assert.Equal(1140, results.Count);
    }

    [Fact]
    public async Task A_known_result_is_read()
    {
        // Arrange
        var seasons = Seasons();

        // Act
        var results = await seasons.GetAsync();

        // Assert
        Assert.Equal(
            new PriorResult(new DateOnly(2023, 8, 11), "Burnley", 0, "Manchester City", 3),
            results[0]);
    }

    [Fact]
    public async Task The_file_is_read_once_and_reused()
    {
        // Arrange
        var seasons = Seasons();
        var first = await seasons.GetAsync();

        // Act
        var second = await seasons.GetAsync();

        // Assert
        Assert.Same(first, second);
    }
}
