#!/usr/bin/env bash
# Atomic conventional commit hook — Process #6 deterministic enforce
# Usage: after each file edit, run: git add <file> && git commit -m "feat/fix/docs: <summary> — <files> — <why>"
set -euo pipefail
FILE="${1:-}"
SUMMARY="${2:-atomic edit}"
WHY="${3:-per user req #6}"
if [ -z "$FILE" ]; then echo "Usage: $0 <file> <summary> <why>"; exit 1; fi
git add "$FILE"
git commit -m "feat/fix/docs: $SUMMARY — $FILE — $WHY"
