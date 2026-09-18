# What we have measured

Findings from running the predictor over gameweeks 2-4 of 2026-27, scored against the
results. Thirty matches, which is small — treat rankings as indicative and single-run
accuracy percentages as nearly meaningless.

## Leakage makes a backtest look brilliant

The first backtest scored **28/30 exact scorelines**. That is not possible. `RelevantHistory`
filtered the season's results by club but not by date, so a fixture that had already been
played appeared in its own history and Jev read the answer off the state.

The fix is one clause — only matches kicking off before the fixture. `WhenPredictingAFixture-
AlreadyPlayed` guards it against the real season.

**Treat any exact-score accuracy much above 15% as a leak, not a result.** The most common
scoreline across the season's first 40 matches occurred 15% of the time.

## An exact scoreline is irreducibly uncertain

Fifty options share the probability, so the best of them rarely holds more than a fifth. That
is honest rather than a tuning failure. Asking narrower questions in the same request —
outcome, over/under 2.5 goals, both teams to score — costs no extra round trip.

Asking the outcome directly scored **43%**, against **50%** for reading it off the top
scoreline. The direct question was not better.

## Historical context made predictions worse

Every combination of state, gameweeks 2-4, scored on Brier (lower is better; a flat third on
every match scores 0.667):

| state sent | Brier | log-loss | outcome | exact |
| --- | ---: | ---: | ---: | ---: |
| flat 1/3 each (not a real run) | **0.667** | — | — | — |
| league averages (not a real run) | 0.729 | — | — | — |
| **this season's form only** | **0.774** | **1.351** | 43% | 7% |
| everything | 0.840 | 1.987 | 43% | 10% |
| nothing | 0.899 | 2.375 | 43% | 20% |
| head-to-head only | 0.968 | 2.239 | 37% | 13% |
| club records only | 0.975 | 3.119 | 47% | 7% |
| **league base rates only** | **1.318** | **5.075** | 30% | 3% |

Reproducible: a second run gave 0.774, 0.840 and 0.891 for the top three, identical to three
decimals.

**This season's form is the only state that earns its place.** Prior-season context — base
rates, club venue records, head-to-head — made things worse, both individually and together.

### Base rates invert

The worst result is the one intended to fix calibration. Given `league.base_rates` saying the
league runs 43.2% home wins and 24.5% draws, Jev replied with an implied **88% home and 1%
draw**, 25 of 30 predictions above 0.9 confidence, and **no draws at all**. It gave the actual
outcome under 5% probability in **18 of 30** matches, against 3 of 30 for form alone.

Handing Jev aggregate percentages does not anchor it. It appears to read the question as
"which club is better" and answer with near-certainty.

## Open problems

- **Nothing beats a flat third.** Every configuration loses to assigning 1/3 to each outcome.
  On this evidence the system has no demonstrated outcome skill.
- **Draws are badly under-predicted.** Form-only implies a 19% draw share against the league's
  24.5%, and predicted 3 draws where 13 happened.
- **The sample is odd.** These 30 matches ran 43% draws and 20% home wins, against a league
  averaging 24.5% and 43.2%. That punishes confident home predictions unusually hard.
- **Jev's `confidence` field does not track correctness.** Across low, middle and high bands it
  scored 43%, 25% and 53%. The winning option's *probability* does carry some signal.

## Reproducing

`scripts/state-experiment.sh` runs each configuration and saves every response; each carries a
`state` array naming the parts it was given. The league blocks are still built and still
tested — they are simply not sent by default, so a future attempt (different instructions, a
larger sample) can re-enable them with a flag rather than rewriting them.
