# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
# Build
dotnet build

# Run tests (Windows only — test project targets net10.0-windows10.0.19041.0)
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~DocumentServiceTests"
dotnet test --filter "FullyQualifiedName~DocumentServiceTests.LoadAsync_WhenFileNotFound"

# Run the app (Windows)
dotnet run --project Kneeboard --framework net10.0-windows10.0.19041.0
```

## Building the installer

```powershell
.\scripts\Publish-Release.ps1
```

`Publish-Release.ps1` cleans, restores, and builds a self-contained x64 exe, then zips the
output into `dist\Kneeboard-v<version>.zip`. Extract the ZIP anywhere and run `Kneeboard.exe`
directly — no installation required.

## Branching

* Always create a new branch before making any changes. Never commit directly to `master`. Never merge or rebase the branch — leave that for the user.
* Name branches using the pattern `<type>/<short-description>`, e.g. `feat/pdf-viewer` or `fix/bootstrap-crash`.
* Always squash commits in a branch into a single commit before merging to `master`. Use `git rebase -i` to squash commits and create a new meaningful commit message.
  * ALWAYS ASK for confirmation of commit message.
  * The commit message should follow the Conventional Commits format.

## Commits

Follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/). Format: `<type>(<scope>): <description>`. Common types: `feat`, `fix`, `refactor`, `test`, `chore`, `docs`.

## Architecture

See [`arch/overview.md`](arch/overview.md).
