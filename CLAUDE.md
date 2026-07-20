# DeckFlow Project Instructions

## Project Overview

DeckFlow is a cross-platform .NET MAUI application (Android + Windows) for managing Magic: The Gathering collections and generating step-by-step card movement instructions for multi-deck play sessions.

## Implementation Plan

The master implementation plan is at:
```
.claude/implementation-plans/2026-02-09-deckflow-comprehensive-plan.md
```

**Always read this file before starting implementation work.**

## Project Index

Check `.claude/project-index.md` before searching for code artifacts. Update the index when adding, renaming, or deleting files.

## Tech Stack

| Component | Technology |
|-----------|------------|
| UI Framework | .NET MAUI XAML |
| MVVM Framework | CommunityToolkit.Mvvm |
| Local Storage | SQLite (sqlite-net-pcl) |
| Target Platforms | Android, Windows |
| .NET Version | net10.0 |

## Project Structure

| Project | Purpose | Dependencies |
|---------|---------|--------------|
| DeckFlow.Core | Domain logic (parsers, planner) | None |
| DeckFlow.Data | SQLite persistence | Core, sqlite-net-pcl |
| DeckFlow | MAUI application | Core, Data, CommunityToolkit.Mvvm |
| DeckFlow.Tests | Unit tests | Core, Data, xUnit |

## Key Design Decisions

1. **Core has zero UI dependencies** - All business logic is UI-agnostic for future web/iOS reuse
2. **Repository pattern** - Interfaces in Data/Abstractions for future backend swap (REST API, EF Core)
3. **Strongly-typed IDs** - `readonly record struct` wrappers (DeckId, LocationId, CardId, PrintingId)
4. **Immutable planning records** - All planner input/output contracts are sealed records
5. **Synchronous planner** - `IMovementPlanner.BuildPlan` is CPU-bound, no async needed
6. **Version 7 GUIDs** - Use `Guid.CreateVersion7()` instead of `Guid.NewGuid()` for all new IDs. Embeds a timestamp for natural chronological ordering, which improves SQLite index performance

## Allocation Priority Rules

When selecting which copy of a card to use:
1. **Printing Match is King** - If deck requests specific printing, find it anywhere
2. **Location Priority Fallback**:
   - a) Unselected deck locations
   - b) Unbound collection locations
   - c) Selected deck locations
3. **Within-tier tiebreakers**: Alphabetical location → higher quantity → older set code
4. **Consume-fully guardrail**: Take all available from source before moving to next

## Sample Data

Located in `.claude/implementation-plans/`:
- `ManaBox_Collection.csv` - 60-row collection export for testing
- `Example_Deck_List.txt` - 100-card 5-color dragon deck for testing

## Build Commands

```bash
# Build entire solution
dotnet build DeckFlow.slnx

# Run tests
dotnet test DeckFlow.Tests/DeckFlow.Tests.csproj

# Run MAUI app (Windows)
dotnet build DeckFlow/DeckFlow.csproj -f net10.0-windows10.0.19041.0

# Run MAUI app (Android emulator)
dotnet build DeckFlow/DeckFlow.csproj -f net10.0-android -t:Run
```

## WSL Notes

This project runs in WSL. When using `dotnet` commands:
- Use WSL paths (`/mnt/c/...`) for native Linux tools
- Use relative paths when possible for `dotnet build`
