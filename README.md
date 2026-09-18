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

### Why the scoreline is not the whole answer

An exact scoreline is irreducibly uncertain. Fifty options share the probability, so the best
of them rarely holds more than a fifth — and that is honest, not a tuning failure. The most
common scoreline across the season's first 40 matches occurred 15% of the time.

So three narrower questions are asked in the same request: the outcome (`choice`, three
options), whether the match passes 2.5 goals (`noul`), and whether both clubs score (`noul`).
Questions run in parallel inside one request, so these cost no extra round trip and reuse the
state we already paid to send. A clean backtest over gameweeks 2-4 scored 20% on exact
scorelines but 53% on the outcome alone.

## Layout

| Project | Holds |
| --- | --- |
| `src/Domain` | `Fixture`, `Prediction`, `MatchPredictor`, `GameweekSchedule`, `RelevantHistory`, and the `IJevPredictor` seam. No HTTP, no JSON files. |
| `src/External` | `JevPredictor` (the only class that speaks Jev's wire format) and the dataset readers. |
| `src/Api` | `GET /gameweeks/{gw}`. |

`data/fixtures.json` and `data/results.json` are this season's datasets; results become the
state sent to Jev, fixtures provide the schedule. `data/prior-seasons.json` holds the last
three completed seasons.

### Anchoring Jev to the league

Jev has no way to know how the Premier League behaves in aggregate, and backtesting showed
it: 43% of its probability went on away wins where the league runs 32%, and draws were
under-weighted. So every request carries a `league` block derived from the three completed
seasons:

| | Last 3 seasons |
| --- | --- |
| Home win | 43.2% |
| Draw | 24.5% |
| Away win | 32.4% |
| Goals per match | 2.99 |
| Over 2.5 goals | 58.8% |
| Both teams to score | 58.3% |

Alongside the rates go the home club's record *at home*, the away club's record *away*, and
the two clubs' previous meetings. Venue is kept separate because that is where most of the
signal is.

#### Clubs with no record

Hull City and Coventry City came up into 2026-27 and appear nowhere in the completed seasons,
which left a hole exactly where a fixture is hardest to call. They stand in for the class they
belong to: the clubs that appear in a season but not the one before it. Across three seasons
that is six promotions — Burnley, Ipswich Town, Leeds United, Leicester City, Southampton and
Sunderland — in the season each came up:

| | Promoted sides | League |
| --- | --- | --- |
| Win at home | 22.8% | 43.2% |
| Win away | 13.2% | 32.4% |
| Conceded at home | 1.71/game | — |
| Conceded away | 2.04/game | — |

A borrowed record carries `"basis": "promoted_sides"` and says so in words, because read as
the club's own it would badly overstate a newcomer's experience. Only the record is borrowed:
previous meetings that never happened are not invented.

Those three seasons are 120 KB of results, so what travels is the summary, not the matches.
`WhenMeasuringTheRealLeaguesBaseRates` pins the rates against the real file.

### Fitting Jev's context limit

Jev accepts **32 KB per request**. The raw results file is 144 KB, and even the matches for a
single pair of clubs are 26 KB with no room to grow. Two things bring that down to ~3.6 KB:

1. **Only the fields that bear on a scoreline are kept.** Commentary (28% of the file),
   substitutions (23%), lineups (10%) and the goal-event list (9%) are dropped when the
   dataset is read. Shots, xG and possession are kept — they are 6% of the bytes and the
   most predictive part.
2. **Only the two clubs involved.** `Domain/History/RelevantHistory` selects matches played
   by either side, most recent first, capped at six per club.

### Backtesting, and not cheating at it

`RelevantHistory` only returns matches that kicked off **before** the fixture being predicted.
This matters because the results dataset holds completed matches: run the predictor over a
round that has already been played and, without the cutoff, the fixture appears in its own
history and Jev reads the answer straight off the state. That looks like a working model — an
early backtest over gameweeks 2-4 scored 28/30 exact scorelines, which is not possible — so
the failure is silent unless you check.

`WhenPredictingAFixtureAlreadyPlayed` guards this against the real season. Treat any
exact-score accuracy much above ~15% as a leak, not a result.

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
      "fixtureId": "espn:401879275",
      "kickoffUtc": "2026-09-18T19:00:00+00:00",
      "homeTeam": "Brentford",
      "awayTeam": "Chelsea",
      "mostLikelyScore": { "home": 1, "away": 2, "confidence": 0.23 },
      "outcome": {
        "result": "AwayWin",
        "homeWin": 0.24, "draw": 0.31, "awayWin": 0.45,
        "confidence": 0.58
      },
      "overTwoAndAHalfGoals": 0.62,
      "bothTeamsToScore": 0.71
    }
  ]
}
```

`mostLikelyScore` and `outcome` are null if Jev returned neither for a fixture. A gameweek
outside the season returns 404.

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
| `WhenMeasuringLeagueBaseRates` / `WhenBuildingTheLeagueContextForAFixture` | The long-run rates and each club's record. |
| `WhenSendingTheLeagueContextToJev` | That the anchor reaches the request. |
| `WhenIdentifyingPromotedSides` / `WhenProfilingAPromotedSide` | The reference class for a club with no record. |
| `WhenAClubHasNoRecordOfItsOwn` / `WhenARealPromotedClubIsInTheFixture` | Standing a newcomer in for it. |
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
