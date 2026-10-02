#!/usr/bin/env bash
# Fails when a src/ project resolves a package with a known advisory, one
# ::error:: line per hit. release.yml's verify job runs it, and so does step 5 of
# CLAUDE.md's release procedure, so a hit surfaces before the tag. Kept out of
# ci.yml: an advisory published upstream would otherwise turn unrelated PRs red.
# Scoped to src/, whose packages build or ship in what reaches nuget.org.
# Run from the repository root.
set -euo pipefail
found=0
for proj in src/*/*.csproj; do
  # With --format json, dotnet list writes its own errors to stdout; jq would
  # swallow them into a bare parse error, so a failed listing is reported first.
  if ! json=$(dotnet list "$proj" package --vulnerable --include-transitive --format json); then
    echo "::error::$proj: dotnet list failed:"
    printf '%s\n' "$json"
    exit 1
  fi
  hits=$(jq -r '.projects[].frameworks[]?
        | (.topLevelPackages // []) + (.transitivePackages // [])
        | .[] | select(.vulnerabilities)
        | "\(.id) \(.resolvedVersion): \([.vulnerabilities[].advisoryurl] | join(" "))"' \
    <<< "$json")
  if [ -n "$hits" ]; then
    while IFS= read -r hit; do echo "::error::$proj uses $hit"; done <<< "$hits"
    found=1
  fi
done
exit $found
