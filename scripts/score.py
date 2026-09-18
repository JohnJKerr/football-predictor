#!/usr/bin/env python3
"""Score saved predictions against results.

    ./scripts/score.py experiments/gw5-predictions.json
    ./scripts/score.py experiments/run/*-gw*.json          # every configuration, compared

Results come from data/results.json plus any lines in experiments/results.csv, which is
where to put fixtures the dataset has not caught up with yet:

    Brentford,3,0,Chelsea
"""
import csv, glob, json, math, os, sys, collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def outcome(h, a):
    return "HomeWin" if h > a else "AwayWin" if a > h else "Draw"


def load_results():
    results = {}
    with open(os.path.join(ROOT, "data", "results.json")) as f:
        for m in json.load(f)["matches"]:
            results[(m["home_team"], m["away_team"])] = (
                m["teams"]["home"]["goals"], m["teams"]["away"]["goals"])

    extra = os.path.join(ROOT, "experiments", "results.csv")
    if os.path.exists(extra):
        with open(extra) as f:
            for row in csv.reader(f):
                if len(row) == 4 and not row[0].startswith("#"):
                    results[(row[0].strip(), row[3].strip())] = (int(row[1]), int(row[2]))
    return results


def score(files, results):
    runs = collections.defaultdict(list)
    for path in files:
        name = os.path.basename(path).replace(".json", "")
        with open(path) as f:
            payload = json.load(f)
        for fx in payload["fixtures"]:
            key = (fx["homeTeam"], fx["awayTeam"])
            if key not in results:
                continue
            hg, ag = results[key]
            actual = outcome(hg, ag)
            o = fx.get("outcome")
            shown = fx.get("scorelines") or (
                [fx["mostLikelyScore"]] if fx.get("mostLikelyScore") else [])
            runs[name].append(dict(
                fixture=f"{fx['homeTeam']} v {fx['awayTeam']}",
                actual=actual, score=(hg, ag),
                H=(o or {}).get("homeWin", 0), D=(o or {}).get("draw", 0),
                A=(o or {}).get("awayWin", 0), called=(o or {}).get("result"),
                top=(shown[0]["home"], shown[0]["away"]) if shown else None,
                in_shown=any((s["home"], s["away"]) == (hg, ag) for s in shown),
                over=fx["overTwoAndAHalfGoals"], over_actual=(hg + ag) > 2.5,
                btts=fx["bothTeamsToScore"], btts_actual=(hg > 0 and ag > 0)))
    return runs


TARGET = {"HomeWin": (1, 0, 0), "Draw": (0, 1, 0), "AwayWin": (0, 0, 1)}


def brier(rows):
    return sum(sum((p - q) ** 2 for p, q in zip((r["H"], r["D"], r["A"]), TARGET[r["actual"]]))
               for r in rows) / len(rows)


def logloss(rows):
    return sum(-math.log(max({"HomeWin": r["H"], "Draw": r["D"], "AwayWin": r["A"]}[r["actual"]], 1e-4))
               for r in rows) / len(rows)


def main(argv):
    files = [p for a in (argv or ["experiments/gw5-predictions.json"]) for p in glob.glob(a)]
    if not files:
        sys.exit("no prediction files matched")

    results = load_results()
    runs = score(files, results)
    if not runs:
        sys.exit("no fixtures with known results -- add them to experiments/results.csv")

    header = f"{'run':<26}{'n':>3}{'Brier':>8}{'logloss':>9}{'outcome':>9}{'top score':>11}{'in top 5':>10}{'O2.5':>7}{'BTTS':>7}"
    print(header)
    print("-" * len(header))
    for name, rows in sorted(runs.items()):
        n = len(rows)
        print(f"{name:<26}{n:>3}{brier(rows):>8.3f}{logloss(rows):>9.3f}"
              f"{sum(r['called'] == r['actual'] for r in rows) / n:>9.0%}"
              f"{sum(r['top'] == r['score'] for r in rows) / n:>11.0%}"
              f"{sum(r['in_shown'] for r in rows) / n:>10.0%}"
              f"{sum((r['over'] > .5) == r['over_actual'] for r in rows) / n:>7.0%}"
              f"{sum((r['btts'] > .5) == r['btts_actual'] for r in rows) / n:>7.0%}")
    print("-" * len(header))
    any_rows = next(iter(runs.values()))
    print(f"{'flat 1/3 each':<26}{len(any_rows):>3}"
          f"{sum(sum((1/3 - q) ** 2 for q in TARGET[r['actual']]) for r in any_rows) / len(any_rows):>8.3f}")

    if len(runs) == 1:
        print()
        rows = next(iter(runs.values()))
        for r in rows:
            p = {"HomeWin": r["H"], "Draw": r["D"], "AwayWin": r["A"]}[r["actual"]]
            mark = "hit " if r["called"] == r["actual"] else "miss"
            print(f"  {mark} {r['fixture'][:44]:<44} {r['score'][0]}-{r['score'][1]}  "
                  f"called {r['called']:<8} p(actual)={p:.0%}"
                  f"{'  exact' if r['top'] == r['score'] else ('  listed' if r['in_shown'] else '')}")


if __name__ == "__main__":
    main(sys.argv[1:])
