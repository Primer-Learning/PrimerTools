# Shared-repo worktree sharing

This repo (and a few others under `Primer/Tools/`) is shared across multiple Godot projects via git worktrees. Each consumer project has its own branch checked out as a worktree inside its `addons/` folder. The goal is to avoid re-importing every shared asset every time Godot opens a different project that uses the content — which happened with the previous symlink setup, because the import cache is per-project and assets looked "new" each time.

## Day-to-day

**Edit assets** directly in the consumer project's worktree — e.g. `YourGame/addons/PrimerAssets/Foo.cs`. No ceremony.

**Sync changes** from inside a consumer worktree:
```
./_sync/sync.sh -m "commit message"
```
This commits pending changes on the consumer branch (e.g. `tacticalrogue`), rebases onto `main`, and advances local `main` to the branch's tip. `-m` is only required if the working tree is dirty.

**No origin push happens automatically.** Run `git push origin main` from any worktree when you want cloud backup.

**Pulling remote changes**:
```
git fetch origin
git rebase origin/main
```

## Setting up a new consumer project

From the canonical checkout of this shared repo (`Primer/Tools/<RepoName>/`):
```
./_sync/add-consumer.sh <consumer-name> /abs/path/to/Project/addons/<FolderName>
```
Then in the consumer project:
1. Add `/addons/<FolderName>/` to `.gitignore`.
2. Commit the `.gitignore` change.

## Architecture

- **`main`** is the integration branch. Advanced locally via `git update-ref` by each consumer's `sync.sh`, not via merge commits.
- **`<consumer-name>`** branches (e.g. `tacticalrogue`) are per-consumer working branches. Every worktree is checked out on its own.
- The **canonical checkout** in `Primer/Tools/<RepoName>/` stays on `main`. Nobody actively edits there — it's just the home of the repo and where `add-consumer.sh` runs from.
- All worktrees share one `.git` object database, so commits made in one worktree are visible locally to the others without any fetching.

## Gotchas

- **`sync.sh` errors out on `main`.** Every worktree must be on a consumer branch.
- **The canonical checkout's working tree can go stale.** When `sync.sh` advances `main` via `update-ref`, a worktree that was on `main` has its HEAD ref follow silently but its files don't update. If you `cd` to the canonical checkout and see weird "deleted" changes in `git status`, run `git reset --hard main` to resync files.
- **Scripts must stay LF.** `_sync/.gitattributes` enforces it on Windows — don't remove.
- **`_sync/` is hidden from Godot** via `.gdignore` so the editor doesn't try to import the scripts.

## Special case: QuaterniusAssets

The `addons/QuaterniusAssets/` directory in consumer projects is a plain container, not a worktree. The actual shared repo (`Stylized Nature MegaKit`) is nested one level deeper. The outer `addons/QuaterniusAssets/` is fully gitignored in consumers; a fresh clone has an empty entry until `add-consumer.sh` populates the inner worktree at `addons/QuaterniusAssets/Stylized Nature MegaKit/`.
