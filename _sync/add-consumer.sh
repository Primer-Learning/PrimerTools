#!/usr/bin/env bash
# Set up this shared repo as a worktree inside a consumer project.
#
# Creates a branch named after the consumer (if needed), then adds a
# worktree at the destination path checked out on that branch.
#
# Usage: _sync/add-consumer.sh <consumer-name> <destination-path>
#
# Example:
#   _sync/add-consumer.sh tacticalrogue \
#     /c/Users/Justin/Documents/Projects/Non-Primer/tacticalrogue/addons/PrimerAssets

set -e

if [ $# -ne 2 ]; then
  echo "Usage: _sync/add-consumer.sh <consumer-name> <destination-path>"
  exit 1
fi

CONSUMER="$1"
DEST="$2"

if git rev-parse --verify "$CONSUMER" >/dev/null 2>&1; then
  echo "Branch '$CONSUMER' already exists."
else
  echo "Creating branch '$CONSUMER' off main..."
  git branch "$CONSUMER" main
fi

if [ -e "$DEST" ] || [ -L "$DEST" ]; then
  echo "Error: '$DEST' already exists. Remove it first (e.g. 'rm' if it's a symlink)."
  exit 1
fi

echo "Creating worktree at '$DEST' on branch '$CONSUMER'..."
git worktree add "$DEST" "$CONSUMER"

echo
echo "Done. Next steps:"
echo "  1. Add the worktree path to the consumer project's .gitignore"
echo "  2. Sync with: '$DEST/_sync/sync.sh' -m \"message\""
