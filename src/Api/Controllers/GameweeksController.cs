namespace Api.Controllers;

using Api.Models;
using Domain.Calibration;
using Domain.Predicting;
using External.Jev;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("gameweeks")]
public sealed class GameweeksController(
    IGameweekPredictor predictor,
    IPredictionLog? log = null,
    IStateSettings? state = null)
    : ControllerBase
{
    /// <summary>Returns the most likely score for every fixture in the given gameweek.</summary>
    [HttpGet("{gameweek:int}")]
    [ProducesResponseType<GameweekResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    /// <param name="scorelines">How many candidate scorelines to report per fixture.</param>
    public async Task<ActionResult<GameweekResponse>> Get(
        int gameweek,
        [FromQuery] int scorelines = GameweekResponseMapper.DefaultScorelines,
        CancellationToken cancellationToken = default)
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

        // Kept so this gameweek can be shown back to Jev once the results are in.
        if (log is not null) await log.RecordAsync(gameweek, predictions, cancellationToken);

        return Ok(predictions.ToResponse(
            gameweek,
            Math.Max(1, scorelines),
            StateSettings.Describe(state ?? StateSettings.Default)));
    }
}
