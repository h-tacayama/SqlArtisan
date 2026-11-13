#!/usr/bin/env bash
# Regenerates the combinatorial SELECT cases from their PICT model. Developer-only: CI never
# runs PICT, and PictModelTests fails the PR when the committed CSV and model disagree.
#
# PICT is pinned to v3.7.4, built from source on first use (needs git, make and a C++
# compiler):
#   git clone --branch v3.7.4 --depth 1 https://github.com/microsoft/pict
#   make -C pict          # produces pict/pict
# Set PICT=/path/to/pict to use a binary built that way instead.
#
# The fixed /r seed makes the output deterministic: rerunning on an unchanged model
# reproduces the committed CSV byte for byte. Run from the repository root.
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
"$PICT" "$models/select.pict" /o:2 "/r:$PICT_SEED" 2>/dev/null | tr '\t' ',' \
  > "$models/select.cases.csv"
echo "$(($(wc -l < "$models/select.cases.csv") - 1)) rows written to $models/select.cases.csv"
