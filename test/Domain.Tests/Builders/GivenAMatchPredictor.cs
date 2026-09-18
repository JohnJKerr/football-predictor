namespace Domain.Tests.Builders;

using Domain.Model;
using Domain.Predicting;

/// <summary>
/// Records how a gameweek drives the per-match predictor: which fixtures it asked about,
/// in what order, and whether any two calls overlapped.
/// </summary>
internal sealed class RecordingMatchPredictor : IMatchPredictor
{
    private int inFlight;

    public List<string> Asked { get; } = [];

    public int PeakInFlight { get; private set; }

    public async Task<IReadOnlyList<Prediction>> PredictAsync(
        Fixture fixture, CancellationToken cancellationToken = default)
    {
        inFlight++;
        PeakInFlight = Math.Max(PeakInFlight, inFlight);
        Asked.Add(fixture.Id);

        await Task.Yield();

        inFlight--;

        // A scoreline derived from the fixture id, so each fixture is distinguishable.
        var goals = int.Parse(fixture.Id.Split('-')[1]);
        return [new Prediction(goals, 0, 0.5)];
    }
}
