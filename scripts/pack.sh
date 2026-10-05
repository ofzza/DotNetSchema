#!/usr/bin/env bash
# Packs the DotNetSchema NuGet package (attribute, build integration and pre-built export tool) into
# artifacts/packages. Usage: ./scripts/pack.sh [extra dotnet pack arguments, e.g. -p:Version=0.2.0]
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet pack "$root/src/DotNetSchema/DotNetSchema.csproj" -c Release -o "$root/artifacts/packages" "$@"
