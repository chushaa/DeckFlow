# DeckFlow Project Index

Last Updated: 2026-02-18
Status: Collections Page Overhaul Complete

---

## Solution Structure

```
DeckFlow.slnx
├── DeckFlow.Core/          net10.0 - Domain logic
├── DeckFlow.Data/          net10.0 - SQLite persistence (refs Core)
├── DeckFlow/               net10.0-android/windows - MAUI app (refs Core, Data)
└── DeckFlow.Tests/         net10.0 - xUnit tests (refs Core, Data)
```

---

## DeckFlow.Core (Domain Logic)

### Value Objects
| Type | File | Description |
|------|------|-------------|
| `DeckId` | `DeckFlow.Core/ValueObjects/Ids.cs` | Strongly-typed deck identifier (Guid) |
| `LocationId` | `DeckFlow.Core/ValueObjects/Ids.cs` | Strongly-typed location identifier (Guid) |
| `CardId` | `DeckFlow.Core/ValueObjects/Ids.cs` | Strongly-typed card identifier (Guid) |
| `PrintingId` | `DeckFlow.Core/ValueObjects/Ids.cs` | Strongly-typed printing identifier (string) |
| `CardRef` | `DeckFlow.Core/ValueObjects/References.cs` | Minimal card identity reference |
| `PrintingRef` | `DeckFlow.Core/ValueObjects/References.cs` | Minimal printing reference |

### Planning Contracts - Enums
| Type | File |
|------|------|
| `PlanStepType` | `DeckFlow.Core/Planning/Contracts/Enums.cs` |
| `SourceLocationKind` | `DeckFlow.Core/Planning/Contracts/Enums.cs` |
| `MoveAction` | `DeckFlow.Core/Planning/Contracts/Enums.cs` |
| `AllocationReason` | `DeckFlow.Core/Planning/Contracts/Enums.cs` |
| `PlannerNoteLevel` | `DeckFlow.Core/Planning/Contracts/Enums.cs` |

### Planning Contracts - Input
| Type | File |
|------|------|
| `PlanRequest` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `DeckSelection` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `DeckDefinition` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `DeckRequirement` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `LocationDefinition` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `InventorySnapshot` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `InventoryStack` | `DeckFlow.Core/Planning/Contracts/Input.cs` |
| `PlanOptions` | `DeckFlow.Core/Planning/Contracts/Input.cs` |

### Planning Contracts - Output
| Type | File |
|------|------|
| `MovementPlan` | `DeckFlow.Core/Planning/Contracts/Output.cs` |
| `DeckPlanContext` | `DeckFlow.Core/Planning/Contracts/Output.cs` |
| `PlanSummary` | `DeckFlow.Core/Planning/Contracts/Output.cs` |

### Planning Contracts - Steps
| Type | File |
|------|------|
| `PlanStep` (abstract) | `DeckFlow.Core/Planning/Contracts/Steps.cs` |
| `PreGameStep` | `DeckFlow.Core/Planning/Contracts/Steps.cs` |
| `TransitionStep` | `DeckFlow.Core/Planning/Contracts/Steps.cs` |
| `FinalReturnStep` | `DeckFlow.Core/Planning/Contracts/Steps.cs` |

### Planning Contracts - Instructions
| Type | File |
|------|------|
| `DeckInstructionSet` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |
| `DeckInstructionSummary` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |
| `SourceLocationGroup` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |
| `MoveLine` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |
| `MissingCardSection` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |
| `MissingCardLine` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |
| `PlannerNote` | `DeckFlow.Core/Planning/Contracts/Instructions.cs` |

### Planning Interface
| Type | File |
|------|------|
| `IMovementPlanner` | `DeckFlow.Core/Planning/IMovementPlanner.cs` |

### Planning Implementation
| Type | File | Description |
|------|------|-------------|
| `AllocationEngine` | `DeckFlow.Core/Planning/Implementation/AllocationEngine.cs` | Card allocation with priority rules |
| `StepBuilder` | `DeckFlow.Core/Planning/Implementation/StepBuilder.cs` | Builds PreGame/Transition/FinalReturn steps |
| `InstructionBuilder` | `DeckFlow.Core/Planning/Implementation/InstructionBuilder.cs` | Groups/sorts instructions by source location |
| `MovementPlanner` | `DeckFlow.Core/Planning/MovementPlanner.cs` | Public IMovementPlanner implementation |

### Parsing
| Type | File | Description |
|------|------|-------------|
| `CardNameHelper` | `DeckFlow.Core/Parsing/CardNameHelper.cs` | Splits DFC names into front/back face |
| `ParsedDeckEntry` | `DeckFlow.Core/Parsing/ParsedDtos.cs` | Single entry from a deck list file |
| `ParsedCollectionEntry` | `DeckFlow.Core/Parsing/ParsedDtos.cs` | Single entry from ManaBox CSV export |
| `DecklistParser` | `DeckFlow.Core/Parsing/DecklistParser.cs` | Plain-text decklist format parser |
| `ManaBoxCsvParser` | `DeckFlow.Core/Parsing/ManaBoxCsvParser.cs` | ManaBox CSV collection parser |

---

## DeckFlow.Data (Persistence)

### Models
| Type | File | Description |
|------|------|-------------|
| `CardModel` | `DeckFlow.Data/Models/CardModel.cs` | Cards table (Name + BackFaceName for DFCs) |
| `PrintingModel` | `DeckFlow.Data/Models/PrintingModel.cs` | Printings table (PK: ScryfallId) |
| `LocationModel` | `DeckFlow.Data/Models/LocationModel.cs` | Locations table (Color, AvailableForDeckAssignment, AllowMultipleDecks) |
| `OwnedCopyModel` | `DeckFlow.Data/Models/OwnedCopyModel.cs` | OwnedCopies table |
| `DeckModel` | `DeckFlow.Data/Models/DeckModel.cs` | Decks table |
| `DeckRequirementModel` | `DeckFlow.Data/Models/DeckRequirementModel.cs` | DeckRequirements table |

### Database
| Type | File | Description |
|------|------|-------------|
| `AppDatabase` | `DeckFlow.Data/Database/AppDatabase.cs` | SQLite singleton, table initialization |

### Abstractions (Interfaces)
| Type | File |
|------|------|
| `ICardRepository` | `DeckFlow.Data/Abstractions/ICardRepository.cs` |
| `IPrintingRepository` | `DeckFlow.Data/Abstractions/IPrintingRepository.cs` |
| `ILocationRepository` | `DeckFlow.Data/Abstractions/ILocationRepository.cs` |
| `IOwnedCopyRepository` | `DeckFlow.Data/Abstractions/IOwnedCopyRepository.cs` |
| `IDeckRepository` | `DeckFlow.Data/Abstractions/IDeckRepository.cs` |
| `IDeckRequirementRepository` | `DeckFlow.Data/Abstractions/IDeckRequirementRepository.cs` |

### Repositories
| Type | File |
|------|------|
| `CardRepository` | `DeckFlow.Data/Repositories/CardRepository.cs` |
| `PrintingRepository` | `DeckFlow.Data/Repositories/PrintingRepository.cs` |
| `LocationRepository` | `DeckFlow.Data/Repositories/LocationRepository.cs` |
| `OwnedCopyRepository` | `DeckFlow.Data/Repositories/OwnedCopyRepository.cs` |
| `DeckRepository` | `DeckFlow.Data/Repositories/DeckRepository.cs` |
| `DeckRequirementRepository` | `DeckFlow.Data/Repositories/DeckRequirementRepository.cs` |

### Services
| Type | File | Description |
|------|------|-------------|
| `IInventoryService` | `DeckFlow.Data/Services/IInventoryService.cs` | Builds PlanRequest from DB |
| `InventoryService` | `DeckFlow.Data/Services/InventoryService.cs` | Implementation |
| `ICollectionImportService` | `DeckFlow.Data/Services/ICollectionImportService.cs` | CSV import |
| `CollectionImportService` | `DeckFlow.Data/Services/CollectionImportService.cs` | Implementation |
| `IDeckImportService` | `DeckFlow.Data/Services/IDeckImportService.cs` | Decklist import + replace |
| `DeckImportService` | `DeckFlow.Data/Services/DeckImportService.cs` | Implementation (ImportAsync + ReplaceRequirementsAsync) |

---

## DeckFlow (MAUI App)

### Views
| Type | File | Description |
|------|------|-------------|
| MainPage | `DeckFlow/MainPage.xaml` | Entry screen: branding, deck count, collection/decks/begin buttons |
| SettingsPage | `DeckFlow/Views/SettingsPage.xaml` | Theme selection (System/Light/Dark) |
| ImportCollectionPage | `DeckFlow/Views/ImportCollectionPage.xaml` | CSV file picker, preview, import flow |
| ImportDeckPage | `DeckFlow/Views/ImportDeckPage.xaml` | Deck file picker, name entry, location binding, import flow |
| DecksPage | `DeckFlow/Views/DecksPage.xaml` | 2-column tile grid of decks with gear icons, import (+) button |
| DeckDetailPage | `DeckFlow/Views/DeckDetailPage.xaml` | Card list for a deck with bound location subtitle |
| DeckSettingsPage | `DeckFlow/Views/DeckSettingsPage.xaml` | Edit name, change location, replace cards, delete deck |
| DeckSelectionPage | `DeckFlow/Views/DeckSelectionPage.xaml` | Deck selection, play order, plan generation |
| StepViewerPage | `DeckFlow/Views/StepViewerPage.xaml` | Step-by-step instruction viewer with progress and navigation |
| CollectionsPage | `DeckFlow/Views/CollectionsPage.xaml` | 2-column binder tile grid, gear icon per tile |
| BinderDetailPage | `DeckFlow/Views/BinderDetailPage.xaml` | Card list for a binder with assigned deck subtitle |
| BinderSettingsPage | `DeckFlow/Views/BinderSettingsPage.xaml` | Color palette picker, deck assignment toggles |

### ViewModels
| Type | File | Description |
|------|------|-------------|
| `MainPageViewModel` | `DeckFlow/ViewModels/MainPageViewModel.cs` | Deck count, navigation commands |
| `ImportCollectionViewModel` | `DeckFlow/ViewModels/ImportCollectionViewModel.cs` | CSV pick/parse/import flow |
| `ImportDeckViewModel` | `DeckFlow/ViewModels/ImportDeckViewModel.cs` | Deck pick/parse/create flow |
| `LocationItem` | `DeckFlow/ViewModels/ImportDeckViewModel.cs` | Location picker item record |
| `DeckTileViewModel` | `DeckFlow/ViewModels/DeckTileViewModel.cs` | Read-only deck tile (name, card count, location) |
| `DecksViewModel` | `DeckFlow/ViewModels/DecksViewModel.cs` | Deck list loading, navigation to detail/settings/import |
| `DeckCardItemViewModel` | `DeckFlow/ViewModels/DeckCardItemViewModel.cs` | Card row data for DeckDetailPage (nullable printing info) |
| `DeckDetailViewModel` | `DeckFlow/ViewModels/DeckDetailViewModel.cs` | Loads deck requirements, card/printing data |
| `DeckSettingsViewModel` | `DeckFlow/ViewModels/DeckSettingsViewModel.cs` | Edit name, location, replace cards, delete deck |
| `DeckItemViewModel` | `DeckFlow/ViewModels/DeckItemViewModel.cs` | Deck card with selection/order state |
| `DeckSelectionViewModel` | `DeckFlow/ViewModels/DeckSelectionViewModel.cs` | Deck selection, reorder, plan generation |
| `StepViewerViewModel` | `DeckFlow/ViewModels/StepViewerViewModel.cs` | Step navigation, progress tracking, IQueryAttributable |
| `BinderTileViewModel` | `DeckFlow/ViewModels/BinderTileViewModel.cs` | Tile data for CollectionsPage grid |
| `CollectionsViewModel` | `DeckFlow/ViewModels/CollectionsViewModel.cs` | Loads binders, handles empty state |
| `BinderCardItemViewModel` | `DeckFlow/ViewModels/BinderCardItemViewModel.cs` | Card row data for BinderDetailPage |
| `BinderDetailViewModel` | `DeckFlow/ViewModels/BinderDetailViewModel.cs` | Loads cards in binder, assigned decks |
| `BinderSettingsViewModel` | `DeckFlow/ViewModels/BinderSettingsViewModel.cs` | Color selection + toggles, save to DB |

### Converters
| Type | File | Description |
|------|------|-------------|
| `InvertedBoolConverter` | `DeckFlow/Converters/BoolConverters.cs` | Negates bool for binding |
| `IsNotNullConverter` | `DeckFlow/Converters/BoolConverters.cs` | True when value is not null |
| `IsNotNullOrEmptyConverter` | `DeckFlow/Converters/BoolConverters.cs` | True when string is not null/empty |
| `StringToColorConverter` | `DeckFlow/Converters/StringToColorConverter.cs` | Converts hex string to MAUI Color |

### Services
| Type | File | Description |
|------|------|-------------|
| `ThemePreference` | `DeckFlow/Services/ThemeService.cs` | Enum: System, Light, Dark |
| `ThemeService` | `DeckFlow/Services/ThemeService.cs` | Persists and applies theme preference |
| `LayoutBreakpoint` | `DeckFlow/Services/LayoutConstants.cs` | Enum: Compact, Medium, Expanded |
| `LayoutConstants` | `DeckFlow/Services/LayoutConstants.cs` | Responsive breakpoints (600/1000) |

### Resources
| File | Description |
|------|-------------|
| `DeckFlow/Resources/Styles/Colors.xaml` | Semantic Light/Dark color palette |
| `DeckFlow/Resources/Styles/Styles.xaml` | Base + named styles (Card, Badge, Button, etc.) |

### Controls
| Type | File | Description |
|------|------|-------------|
| `DeckCard` | `DeckFlow/Controls/DeckCard.xaml` | Reusable deck card with name, location, selection indicator, order badge |
| `SourceLocationGroupControl` | `DeckFlow/Controls/SourceLocationGroupControl.xaml` | Location group with header, card count badge, move lines |
| `MoveLineControl` | `DeckFlow/Controls/MoveLineControl.xaml` | Single card move line with quantity, name, foil indicator, set code |
| `MissingCardsSectionControl` | `DeckFlow/Controls/MissingCardsSectionControl.xaml` | Error-styled missing cards list |

---

## DeckFlow.Tests (Unit Tests)

| Category | File | Description |
|----------|------|-------------|
| `TestData` | `DeckFlow.Tests/Helpers/TestData.cs` | Scenario builders (5 scenarios + helper methods) |
| Planner Scenarios | `DeckFlow.Tests/Planning/MovementPlanner_Scenarios_Tests.cs` | Core scenario tests (8 tests) |
| Planner Edge Cases | `DeckFlow.Tests/Planning/MovementPlanner_EdgeCase_Tests.cs` | Edge case tests (10 tests) |
| CardNameHelper | `DeckFlow.Tests/Parsing/CardNameHelper_Tests.cs` | DFC name splitting tests (9 tests) |
| DecklistParser | `DeckFlow.Tests/Parsing/DecklistParser_Tests.cs` | Decklist parser tests (16 tests) |
| ManaBoxCsvParser | `DeckFlow.Tests/Parsing/ManaBoxCsvParser_Tests.cs` | CSV parser tests (16 tests) |

---

## Update Log

| Date | Change |
|------|--------|
| 2026-02-09 | Initial index created |
| 2026-02-09 | Phase 1 complete: all projects, value objects, planning contracts, IMovementPlanner |
| 2026-02-09 | Phase 2 complete: ParsedDtos, DecklistParser, ManaBoxCsvParser |
| 2026-02-09 | Phase 3 complete: all models, AppDatabase, repositories, services (using Guid.CreateVersion7) |
| 2026-02-09 | Phase 4 complete: AllocationEngine, StepBuilder, InstructionBuilder, MovementPlanner |
| 2026-02-09 | Phase 5 complete: 46 unit tests (TestData, 8 scenarios, 10 edge cases, 14 parser each) |
| 2026-02-09 | Phase 6 complete: Colors.xaml, Styles.xaml, ThemeService, SettingsPage, LayoutConstants |
| 2026-02-09 | Phase 7 complete: MainPage, ImportCollectionPage, ImportDeckPage, ViewModels, Converters, DI wiring |
| 2026-02-11 | Phase 8 complete: DeckSelectionPage, DeckItemViewModel, DeckSelectionViewModel, DeckCard control, IsNotNullConverter |
| 2026-02-11 | Phase 9 complete: StepViewerPage, StepViewerViewModel, SourceLocationGroupControl, MoveLineControl, MissingCardsSectionControl |
| 2026-02-11 | Phase 10 complete: Accessibility improvements (touch targets, semantic properties), zero warnings, plan/index updated |
| 2026-02-17 | Deck Management: DecksPage, DeckEditPage, DeckTileViewModel, DecksViewModel, DeckEditViewModel, ReplaceRequirementsAsync |
| 2026-02-18 | Collections Overhaul: CollectionsPage, BinderDetailPage, BinderSettingsPage, binder tiles, color/assignment settings, location picker filtering |
| 2026-02-18 | Deck Pages Refactor: DeckEditPage → DeckSettingsPage rename, new DeckDetailPage/DeckDetailViewModel/DeckCardItemViewModel, gear icons on deck tiles |