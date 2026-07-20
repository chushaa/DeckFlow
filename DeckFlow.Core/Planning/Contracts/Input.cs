namespace DeckFlow.Core.Planning.Contracts;

using DeckFlow.Core.ValueObjects;

public sealed record PlanRequest(
	DateTime RequestedUtc,
	IReadOnlyList<DeckSelection> SelectedDecksInOrder,
	IReadOnlyList<DeckDefinition> AllDecks,
	IReadOnlyList<LocationDefinition> Locations,
	InventorySnapshot Inventory,
	PlanOptions Options);

public sealed record DeckSelection(DeckId DeckId, int OrderIndex);

public sealed record DeckDefinition(
	DeckId Id,
	string Name,
	LocationId? BoundLocationId,
	IReadOnlyList<DeckRequirement> Requirements);

public sealed record DeckRequirement(
	CardRef Card,
	int Quantity,
	PrintingRef? RequestedPrinting);

public sealed record LocationDefinition(
	LocationId Id,
	string Name,
	SourceLocationKind Kind,
	DeckId? BoundDeckId);

public sealed record InventorySnapshot(
	IReadOnlyList<InventoryStack> Stacks);

public sealed record InventoryStack(
	LocationId LocationId,
	CardRef Card,
	PrintingRef Printing,
	int Quantity);

public sealed record PlanOptions(
	bool PrintingMatchOverridesLocationPriority = true,
	bool ConsumeFullyFromSourceBeforeNext = true,
	bool SortLocationGroupsAlphabetically = true,
	bool SortCardsAlphabetically = true,
	bool IncludeMissingCardsSection = true);