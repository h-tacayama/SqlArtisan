#!/usr/bin/env bash
# Regenerates llms-full.txt from llms.txt's linked pages, in llms.txt's order.
# LlmsFullTests gates the result byte-for-byte. Run from the repository root.
set -euo pipefail
{ printf '%s\n' "# SqlArtisan — Full Documentation" "" \
    "> The full-text companion to llms.txt: every page it links via a raw-content" \
    "> URL, concatenated in that same order, for AI tools that ingest one file" \
    "> instead of following links. See llms.txt first for the short index; this" \
    "> file is the deep-ingestion counterpart."; \
  for f in README.md docs/README.md docs/query-statements.md docs/expressions.md \
      docs/functions.md docs/analyzer.md docs/cookbook.md docs/comparison.md \
      docs/guides/dapper-quickstart.md docs/guides/oracle-array-bind.md \
      docs/guides/ai-assistants.md \
      docs/versioning.md; do \
    printf '\n<!-- %s -->\n<!-- SOURCE: %s -->\n<!-- %s -->\n\n' \
      '============================================================' "$f" \
      '============================================================'; \
    cat "$f"; \
  done; } > llms-full.txt
