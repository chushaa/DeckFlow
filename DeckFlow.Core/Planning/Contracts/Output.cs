namespace DeckFlow.Core.Planning.Contracts;

using DeckFlow.Core.ValueObjects;

public sealed record MovementPlan(
	Guid PlanId,
	DateTime CreatedUtc,
	IReadOnlyList<DeckPlanContext> SelectedDecksInOrder,
	IReadOnlyList<PlanStep> Steps,
	PlanSummary Summary);

public sealed record DeckPlanContext(
	DeckId DeckId,
	string DeckName,
	LocationId? BoundLocationId,
	string? BoundLocationName,
	int OrderIndex);

public sealed record PlanSummary(
	int TotalSteps,
	int TotalMoveLines,
	int TotalCardsToMove,
	int TotalMissingCards);