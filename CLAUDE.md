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

## Workflow

Before starting any implementation task, invoke the `superpowers:using-git-worktrees` skill to create an isolated branch. Do not merge or push after finishing — commit the work and stop.

## Commits

Follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/). Format: `<type>(<scope>): <description>`. Common types: `feat`, `fix`, `refactor`, `test`, `chore`, `docs`.

## Architecture

See [`arch/overview.md`](arch/overview.md).
