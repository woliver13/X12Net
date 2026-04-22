#!/usr/bin/env bash
# coverage.sh — generate an HTML code coverage report for all test projects.
#
# Usage:
#   bash scripts/coverage.sh            # run all tests, open report
#   bash scripts/coverage.sh --no-open  # run all tests, skip auto-open
#
# Output: coverage/html/index.html
# Requires: dotnet tool restore (installs ReportGenerator from .config/dotnet-tools.json)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RAW_DIR="$REPO_ROOT/coverage/raw"
HTML_DIR="$REPO_ROOT/coverage/html"
OPEN_REPORT=true

for arg in "$@"; do
  [[ "$arg" == "--no-open" ]] && OPEN_REPORT=false
done

echo "==> Restoring dotnet tools..."
dotnet tool restore

echo "==> Cleaning previous coverage data..."
rm -rf "$RAW_DIR"

echo "==> Running tests with coverage collection..."
dotnet test "$REPO_ROOT/X12Net.sln" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RAW_DIR" \
  --nologo

echo "==> Generating HTML report..."
dotnet tool run reportgenerator \
  -reports:"$RAW_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$HTML_DIR" \
  -reporttypes:"Html;TextSummary" \
  -assemblyfilters:"+X12Net;+HL7Net" \
  -verbosity:Warning

echo ""
echo "Coverage summary:"
cat "$HTML_DIR/Summary.txt" 2>/dev/null || true

echo ""
echo "Full report: $HTML_DIR/index.html"

if $OPEN_REPORT; then
  # Open in default browser — works on Windows (Git Bash), macOS, and Linux
  if command -v explorer.exe &>/dev/null; then
    explorer.exe "$(cygpath -w "$HTML_DIR/index.html")"
  elif command -v open &>/dev/null; then
    open "$HTML_DIR/index.html"
  elif command -v xdg-open &>/dev/null; then
    xdg-open "$HTML_DIR/index.html"
  fi
fi
