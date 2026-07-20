namespace DeckFlow.Core.Planning.Contracts;

using DeckFlow.Core.ValueObjects;

public sealed record DeckInstructionSet(
	DeckId DeckId,
	string DeckName,
	IReadOnlyList<SourceLocationGroup> SourceGroups,
	MissingCardSection? MissingCards,
	DeckInstructionSummary Summary);

public sealed record DeckInstructionSummary(
	int MoveLineCount,
	int TotalCardQuantity,
	int MissingCardCount);

public sealed record SourceLocationGroup(
	LocationId LocationId,
	string LocationName,
	SourceLocationKind LocationKind,
	IReadOnlyList<MoveLine> MoveLines,
	int SortKey);

public sealed record MoveLine(
	CardRef Card,
	PrintingRef ActualPrinting,
	PrintingRef? RequestedPrinting,
	int Quantity,
	MoveAction Action,
	LocationId SourceLocationId,
	string SourceLocationName,
	DeckId DestinationDeckId,
	string DestinationDeckName,
	LocationId? DestinationLocationId,
	AllocationReason Reason,
	int SortKey);

public sealed record MissingCardSection(
	IReadOnlyList<MissingCardLine> Cards);

public sealed record MissingCardLine(
	CardRef Card,
	PrintingRef? RequestedPrinting,
	int QuantityNeeded,
	int SortKey);

public sealed record PlannerNote(
	PlannerNoteLevel Level,
	string Code,
	string Message);