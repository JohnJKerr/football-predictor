namespace Api.Controllers;

using Api.Models;
using Domain.Predicting;
using External.Jev;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("gameweeks")]
public sealed class GameweeksController(IGameweekPredictor predictor, IStateSettings? state = null)
    : ControllerBase
{
    /// <summary>Returns the most likely score for every fixture in the given gameweek.</summary>
    [HttpGet("{gameweek:int}")]
    [ProducesResponseType<GameweekResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameweekResponse>> Get(
        int gameweek, CancellationToken cancellationToken = default)
    {
        var predictions = await predictor.PredictAsync(gameweek, cancellationToken);

        if (predictions.Count == 0)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Gameweek not found",
                Detail = $"Gameweek {gameweek} is not part of the 2026-27 season.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return Ok(predictions.ToResponse(gameweek, StateSettings.Describe(state ?? StateSettings.Default)));
    }
}
