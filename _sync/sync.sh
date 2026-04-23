#!/usr/bin/env bash
# Sync this worktree's consumer branch with local main.
#
# Commits any pending changes on the current branch, rebases onto main,
# then advances main to the tip of this branch. Local only — does not push.
#
# Usage: _sync/sync.sh -m "commit message"
#        (message required only if the working tree is dirty)

set -e

MSG=""
while getopts "m:h" opt; do
  case $opt in
    m) MSG="$OPTARG" ;;
    h)
      echo "Usage: _sync/sync.sh -m \"commit message\""
      echo
      echo "Commits pending changes on the current (consumer) branch, rebases"
      echo "onto main, then advances main to this branch's tip. Local only."
      exit 0
      ;;
    *) exit 1 ;;
  esac
done

BRANCH=$(git rev-parse --abbrev-ref HEAD)

if [ "$BRANCH" = "main" ]; then
  echo "Error: on main. sync.sh runs from a consumer branch."
  echo "Each worktree (including this one) should be on its own branch."
  exit 1
fi

if [ "$BRANCH" = "HEAD" ]; then
  echo "Error: detached HEAD. sync.sh needs a named consumer branch."
  exit 1
fi

DIRTY=false
if [ -n "$(git status --porcelain)" ]; then
  DIRTY=true
fi

if $DIRTY; then
  if [ -z "$MSG" ]; then
    echo "Error: -m \"commit message\" required (working tree has changes)."
    exit 1
  fi
  echo "Committing changes on $BRANCH..."
  git add -A
  git commit -m "$MSG"
fi

echo "Rebasing $BRANCH onto main..."
git rebase main

NEW=$(git rev-parse HEAD)
OLD=$(git rev-parse main)
if [ "$NEW" != "$OLD" ]; then
  echo "Advancing main: $(git rev-parse --short main) -> $(git rev-parse --short HEAD)"
  git update-ref refs/heads/main HEAD
else
  echo "main already at $(git rev-parse --short HEAD); nothing to advance."
fi

echo "Done. Push to origin manually when ready."
