# Football Predictor

A proof of concept that predicts Premier League scorelines with
[Jev](https://docs.typesafe.ai/api) (TypeSafe System One).

## How it asks Jev

Jev is a *typed question* API, not a text model. Rather than asking two "expected goals"
questions and multiplying the answers together — which would assume home and away goals are
independent — this asks a **single `choice` question whose options are the scorelines
themselves**:

```jsonc
POST https://api.typesafe.ai/v1/systemone
Authorization: Bearer <key>

{
  "state":  { "fixture": { /* the match */ }, "history": [ /* recent form, both clubs */ ] },
  "model":  "jev-latest",
  "questions": {
    "scoreline": {
      "type": "choice",
      "instructions": "... what is the likely full-time score?",
      "criteria": { "0-0": null, "1-0": null, /* ... 49 in total */ "other": "..." }
    }
  }
}
```

Jev replies with a probability for every scoreline, so `Prediction.Confidence` is Jev's own
probability for that exact result rather than a number we invented.

## Layout

| Project | Holds |
| --- | --- |
| `src/Domain` | `Fixture`, `Prediction`, `MatchPredictor`, `GameweekSchedule`, `RelevantHistory`, and the `IJevPredictor` seam. No HTTP, no JSON files. |
| `src/External` | `JevPredictor` (the only class that speaks Jev's wire format) and the dataset readers. |
| `src/Api` | `GET /gameweeks/{gw}`. |

`data/fixtures.json` and `data/results.json` are the season datasets; results become the
state sent to Jev, fixtures provide the schedule.

### Fitting Jev's context limit

Jev accepts **32 KB per request**. The raw results file is 144 KB, and even the matches for a
single pair of clubs are 26 KB with no room to grow. Two things bring that down to ~3.6 KB:

1. **Only the fields that bear on a scoreline are kept.** Commentary (28% of the file),
   substitutions (23%), lineups (10%) and the goal-event list (9%) are dropped when the
   dataset is read. Shots, xG and possession are kept — they are 6% of the bytes and the
   most predictive part.
2. **Only the two clubs involved.** `Domain/History/RelevantHistory` selects matches played
   by either side, most recent first, capped at six per club.

`JevPredictor.MaxRequestBytes` is a transport backstop: if a request would still exceed 32 KB
it sheds the oldest matches until it fits, so a long season cannot silently produce a
rejected request.

### Gameweeks

The fixture dataset has no matchweek field, so a gameweek is derived as a block of ten
consecutive fixtures in kickoff order. `JsonFileFixtureSourceTests` asserts this yields each
club exactly once across all 38 gameweeks of the real season.

## Running it

```bash
dotnet user-secrets set "Jev:ApiKey" "<your key>" --project src/Api
dotnet run --project src/Api
curl http://localhost:5270/gameweeks/5
```

```jsonc
{
  "gameweek": 5,
  "fixtures": [
    {
      "fixtureId": "espn:401879279",
      "kickoffUtc": "2026-09-18T19:00:00+00:00",
      "homeTeam": "Brentford",
      "awayTeam": "Chelsea",
      "mostLikelyScore": { "home": 1, "away": 2, "confidence": 0.23 }
    }
  ]
}
```

`mostLikelyScore` is null if Jev returns no scoreline for a fixture. A gameweek outside the
season returns 404.

## Tests

```bash
dotnet test
```

Suites are grouped by behaviour, one class per context:

| Context | Covers |
| --- | --- |
| `WhenPredictingAMatch` | Ranking scorelines, confidence, dropping the catch-all. |
| `WhenPredictingAGameweek` | Predicting each fixture, one at a time, in kickoff order. |
| `WhenPlacingFixturesIntoGameweeks` | Blocks of ten, kickoff order, stable ties. |
| `WhenSelectingRelevantHistory` | Which matches are worth sending to Jev. |
| `WhenAskingJevForAScoreline` | The request shape: endpoint, auth, question, options. |
| `WhenBuildingTheStateSentToJev` | What goes in `state`, and the 32 KB budget. |
| `WhenReadingJevsAnswer` | Parsing probabilities; failures surfacing. |
| `WhenRetrievingTheSeasonFixtures` / `WhenRetrievingHistory` | Reading the datasets. |
| `WhenDerivingGameweeksFromTheRealSchedule` | The derivation, against the real season. |
| `WhenRequestingAGameweek` | Controller logic in isolation. |
| `WhenCallingTheGameweekEndpoint` | In-process host against **the real Jev**; skips itself when no `Jev:ApiKey` is configured. |

## Known rough edges

- Fixtures are predicted one at a time. There is no retry or backoff if Jev rate-limits (429).
- A Jev failure surfaces as a 500 with no detail.
