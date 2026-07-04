#!/bin/bash
# SessionStart hook for Claude Code on the web.
# Ensures the .NET 10 SDK pinned by global.json is available and NuGet packages are restored so that
# `dotnet build` / `dotnet test` work immediately in remote sessions.
set -euo pipefail

# Only run in Claude Code on the web (remote env). Locally we assume the
# developer already manages their own .NET SDK.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
    exit 0
fi

PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"

SUDO=""
if [ "$(id -u)" -ne 0 ]; then
    SUDO="sudo"
fi

# Install the .NET 10 SDK from the Ubuntu archive if it isn't already present.
# (The official dot.net / Azure CDN installers are blocked by the network
# policy in this environment, but archive.ubuntu.com is reachable.)
if ! command -v dotnet >/dev/null 2>&1; then
    echo "Installing .NET 10 SDK..."
    export DEBIAN_FRONTEND=noninteractive
    # Refresh package lists; tolerate failures from unrelated third-party PPAs.
    $SUDO apt-get update -qq || true
    $SUDO apt-get install -y --no-install-recommends dotnet-sdk-10.0
fi

# Quiet the first-run banner and telemetry for this and the session's commands.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    {
        echo 'export DOTNET_NOLOGO=1'
        echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    } >> "$CLAUDE_ENV_FILE"
fi

echo "dotnet $(dotnet --version)"

# Restore NuGet packages so the first build/test in the session is fast.
dotnet restore "$PROJECT_DIR/SqlArtisan.sln"
