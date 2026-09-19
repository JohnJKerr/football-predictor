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

### Summarising form does the same thing

The same failure repeated with a block designed to avoid it. `form` states each club's own
recent window as rates — goals and xG, scored and conceded, per match — drawn from exactly
the matches already being sent, adding no new facts. It was added because xG is a measurably
steadier signal than goals over a short window: across three completed seasons, a club's
first-half-of-season xG predicts its second-half goals at r=+0.62, against +0.57 for goals
themselves.

Gameweeks 2-4 again, same 30 matches:

| state sent | Brier | log-loss | outcome | implied draw | over 0.9 confidence |
| --- | ---: | ---: | ---: | ---: | ---: |
| **this season's form, per match** | **0.751** | **1.302** | 40% | 20% | 7 of 30 |
| form, plus the same window as rates | 0.981 | 1.802 | 37% | 10% | 15 of 30 |
| the rates alone, no per-match detail | 1.033 | 2.600 | 37% | 7% | 16 of 30 |

Form-only scored 0.751 here against 0.774 in the earlier run, so the harness is stable and
the gap is far larger than run-to-run variation.

Note the last two columns. Adding the rates halves the implied draw share and doubles the
predictions above 0.9; removing the per-match detail and leaving only the rates goes further
in the same direction. This is the base-rates failure in miniature, from a club's own figures
rather than the league's.

**The statistic being good does not survive contact with how Jev uses it.** xG really is the
steadier measure. Presented as a rate, it still cost 0.23 Brier.

### Every aggregate has lost

Four independent attempts, one pattern:

| aggregate handed to Jev | Brier | what it did to the answers |
| --- | ---: | --- |
| league base rates | 1.318 | 88% home implied, no draws at all |
| the club's own form as rates | 0.981 | draws halved, confidence doubled |
| club venue records | 0.975 | — |
| head-to-head record | 0.968 | — |
| *(none — raw per-match history)* | **0.751** | — |

The first row and the last two come from the earlier run, where form-only scored 0.774 rather
than 0.751; the rows are close enough to rank but not to subtract.

Given a summary number, Jev becomes confident and stops predicting draws. Given a list of
matches, it does not. **Prefer per-match or per-event detail over anything derived.** A new
state block should be treated as likely to lose until measured, and kept behind an
`IStateSettings` flag defaulting to off.

## What the data says before Jev is asked

Three ideas were tested against the three completed seasons directly, rather than by running
the predictor. None reached the point of being worth building. Scripts are not kept; the
figures below are the record.

- **Goal types don't identify a club vulnerability.** 3406 goal events. A club's share of
  goals conceded by type does not carry from one season to the next — headers r=-0.13,
  penalties r=+0.01 over 34 club-season pairs — while total goals conceded carries at
  r=+0.63. Club spread is consistent with noise (header z=+1.12), implying a true
  between-club SD of about 2 points around a 16.5% league share. The two datasets also
  disagree on vocabulary: `data/results.json` has `set_piece` and no `volley`, the
  prior-seasons file the reverse, so set-piece vulnerability is not measurable from history.
- **Head-to-head is indicative but redundant.** Over 578 meetings, the prior H2H record
  correlates +0.30 with the next meeting's xGD — but +0.79 with overall club strength, and
  partialling strength out leaves **-0.08**. Once Jev knows which club is stronger, the
  meeting history adds nothing. Exact scorelines and xG would be the same information at
  finer grain. This is well-powered and is the likeliest explanation for head-to-head scoring
  0.968.
- **"Players who always score against X" is Poisson noise.** 60 players with 15+ goals, 255
  player-opponent cells: chi2=233 on 255 (z=-0.97), i.e. slightly *narrower* than chance. A
  player's surplus against a club in 2023-25 correlates +0.04 with 2025-26. The extremes look
  convincing — Haaland 8 against West Ham on 4.0 expected — but across 255 cells several such
  outliers are expected, and none persist. Independently blocked anyway:
  `data/fixtures.json` carries no lineups, so for an upcoming fixture we do not know who is
  playing.

## Open problems

- **Nothing beats a flat third.** Every configuration loses to assigning 1/3 to each outcome.
  On this evidence the system has no demonstrated outcome skill.
- **Draws are badly under-predicted.** Form-only implies a 19% draw share against the league's
  24.5%, and predicted 3 draws where 13 happened.
- **The sample is odd.** These 30 matches ran 43% draws and 20% home wins, against a league
  averaging 24.5% and 43.2%. That punishes confident home predictions unusually hard.
- **Jev does not know home advantage exists.** Across 34 scored matches its implied rates run
  37% home / 20% draw / 43% away, against league base rates of 43.2 / 24.5 / 32.4 — it makes
  the away side the favourite on average where the league favours the home side by eleven
  points. Untested, and worth one flag rather than an assumption: stating the fact in the
  preamble is the same shape as every aggregate that has lost.
- **Jev's `confidence` field does not track correctness.** Across low, middle and high bands it
  scored 43%, 25% and 53%. The winning option's *probability* does carry some signal.

## Reproducing

`scripts/state-experiment.sh` runs each configuration and saves every response; each carries a
`state` array naming the parts it was given. The league blocks and `form` are still built and
still tested — they are simply not sent by default, so a future attempt (different
instructions, a larger sample) can re-enable them with a flag rather than rewriting them.

`scripts/score.py` scores saved responses against results:

    ./scripts/score.py experiments/gw5-predictions.json
    ./scripts/score.py experiments/<run>/*-gw*.json      # configurations side by side

Results come from `data/results.json`, plus `experiments/results.csv` for fixtures the dataset
has not caught up with — one `Home,HomeGoals,AwayGoals,Away` line each, club names spelled as
`data/fixtures.json` spells them.

### Where gameweek 5 stands

`experiments/gw5-predictions.json` holds ten predictions made on form alone. Four have been
played and entered: 1 of 4 outcomes, Brier 0.684. Three of the four were home wins and Jev
called away in each of the three it missed. Six results still to come, and four matches says
very little on its own — the figure to look at is the full ten.
