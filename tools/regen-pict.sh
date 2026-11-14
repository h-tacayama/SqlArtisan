#!/usr/bin/env bash
# Regenerates every combinatorial model's cases (Models/<name>.pict -> <name>.cases.csv).
# Developer-only: CI never runs PICT; PictModelTests holds each committed CSV to its model's
# levels, constraints and pairs, not to PICT's exact output, so rerun this after every edit.
#
# PICT is pinned to v3.7.4, built from source on first use (needs git, make and a C++
# compiler):
#   git clone --branch v3.7.4 --depth 1 https://github.com/microsoft/pict
#   make -C pict          # produces pict/pict
# Set PICT=/path/to/pict to use a binary built that way instead.
#
# The fixed /r seed makes the output deterministic: rerunning on an unchanged model
# reproduces its committed CSV byte for byte, so regenerating all of them is safe. Run from
# the repository root.
set -euo pipefail

PICT_TAG=v3.7.4
PICT_SEED=1113
models=tests/SqlArtisan.IntegrationTests/Randomized/Models

if [ -z "${PICT:-}" ]; then
  build="${XDG_CACHE_HOME:-$HOME/.cache}/sqlartisan-pict/$PICT_TAG"
  if [ ! -x "$build/pict" ]; then
    rm -rf "$build"
    git -c advice.detachedHead=false clone --quiet --branch "$PICT_TAG" --depth 1 \
      https://github.com/microsoft/pict "$build"
    make -C "$build" --quiet
  fi
  PICT="$build/pict"
fi

# PICT writes tab-separated rows (and its seed to stderr); the cases file is comma-separated.
for model in "$models"/*.pict; do
  cases="${model%.pict}.cases.csv"
  "$PICT" "$model" /o:2 "/r:$PICT_SEED" 2>/dev/null | tr '\t' ',' > "$cases"
  echo "$(($(wc -l < "$cases") - 1)) rows written to $cases"
done
