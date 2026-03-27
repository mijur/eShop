#!/usr/bin/env bash
# Validates an ADR file for required structure and content.
# Usage: bash validate-adr.sh <path-to-adr.md>
# Exit 0 = valid, Exit 1 = invalid (prints what's missing)

set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: validate-adr.sh <path-to-adr.md>"
  exit 1
fi

FILE="$1"
ERRORS=()

if [[ ! -f "$FILE" ]]; then
  echo "FAIL: File not found: $FILE"
  exit 1
fi

# Check file is in docs/adr/
if [[ "$FILE" != *docs/adr/* ]]; then
  ERRORS+=("File is not in docs/adr/ directory")
fi

# Required top-level sections
for section in "## Context" "## Decision" "## Alternatives Considered" "## Consequences"; do
  if ! grep -q "^${section}" "$FILE"; then
    ERRORS+=("Missing required section: ${section}")
  fi
done

# Required subsections under Consequences
for subsection in "### Positive" "### Negative"; do
  if ! grep -q "^${subsection}" "$FILE"; then
    ERRORS+=("Missing required Consequences subsection: ${subsection}")
  fi
done

# Context must contain file references (paths like src/ or tests/ or common path patterns)
CONTEXT_BLOCK=$(sed -n '/^## Context/,/^## /p' "$FILE" | head -n -1)
if ! echo "$CONTEXT_BLOCK" | grep -qE '(src/|tests/|\.csproj|\.cs|\.proto|\.json)'; then
  ERRORS+=("Context section must reference specific files in the codebase (e.g., src/... paths)")
fi

# Report results
if [[ ${#ERRORS[@]} -eq 0 ]]; then
  echo "OK: $FILE passes all validation checks."
  exit 0
else
  echo "FAIL: $FILE has ${#ERRORS[@]} issue(s):"
  for err in "${ERRORS[@]}"; do
    echo "  - $err"
  done
  exit 1
fi
