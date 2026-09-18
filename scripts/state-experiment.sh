#!/usr/bin/env bash
# TEMPORARY. Runs the gameweek endpoint once per state configuration and saves each
# response, so the parts of the state Jev is given can be compared on the same fixtures.
#
#   ./scripts/state-experiment.sh                  # every configuration, gameweeks 2-4
#   ./scripts/state-experiment.sh none all         # just those two configurations
#   GAMEWEEKS="2 3 4 5" ./scripts/state-experiment.sh
#
# Needs the Jev key in user secrets:
#   dotnet user-secrets set "Jev:ApiKey" "<key>" --project src/Api
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${OUT:-$ROOT/experiments/$(date +%Y%m%d-%H%M%S)}"
PORT="${PORT:-5299}"
GAMEWEEKS="${GAMEWEEKS:-2 3 4}"

# name:form:rates:records:h2h
CONFIGS=(
  "none:false:false:false:false"
  "form-only:true:false:false:false"
  "base-rates-only:false:true:false:false"
  "club-records-only:false:false:true:false"
  "head-to-head-only:false:false:false:true"
  "all:true:true:true:true"
)

wanted=("$@")
selected() {
  [ ${#wanted[@]} -eq 0 ] && return 0
  for w in "${wanted[@]}"; do [ "$w" = "$1" ] && return 0; done
  return 1
}

stop_api() {
  [ -n "${API_PID:-}" ] && kill "$API_PID" 2>/dev/null
  wait "$API_PID" 2>/dev/null
  API_PID=""
}
trap stop_api EXIT

mkdir -p "$OUT"
echo "Building..."
dotnet build "$ROOT/src/Api" --nologo -v q || exit 1

for config in "${CONFIGS[@]}"; do
  IFS=: read -r name form rates records h2h <<< "$config"
  selected "$name" || continue

  echo
  echo "=== $name (form=$form rates=$rates records=$records h2h=$h2h) ==="

  ASPNETCORE_ENVIRONMENT=Development \
  State__IncludeRecentForm="$form" \
  State__IncludeBaseRates="$rates" \
  State__IncludeClubRecords="$records" \
  State__IncludeHeadToHead="$h2h" \
    dotnet "$ROOT/src/Api/bin/Debug/net10.0/Api.dll" --urls "http://127.0.0.1:$PORT" \
    > "$OUT/$name.log" 2>&1 &
  API_PID=$!

  ready=""
  for _ in $(seq 1 40); do
    sleep 1
    curl -sf -o /dev/null "http://127.0.0.1:$PORT/gameweeks/99" 2>/dev/null
    [ $? -ne 7 ] && ready=1 && break
  done
  if [ -z "$ready" ]; then
    echo "  API did not start; see $OUT/$name.log"
    stop_api
    continue
  fi

  for gw in $GAMEWEEKS; do
    file="$OUT/$name-gw$gw.json"
    code=$(curl -s -o "$file" -w '%{http_code}' --max-time 600 "http://127.0.0.1:$PORT/gameweeks/$gw")
    if [ "$code" = "200" ]; then
      echo "  gameweek $gw -> $(basename "$file")"
    else
      echo "  gameweek $gw FAILED (HTTP $code) -- $(head -c 200 "$file")"
    fi
  done

  stop_api
done

echo
echo "Saved to $OUT"
