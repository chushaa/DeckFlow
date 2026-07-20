namespace DeckFlow.Core.Planning;

using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.Planning.Implementation;
using DeckFlow.Core.ValueObjects;

/// <summary>
/// Generates movement plans for multi-deck play sessions.
/// Orchestrates allocation, step building, and instruction grouping.
/// </summary>
public sealed class MovementPlanner : IMovementPlanner
{
	public MovementPlan BuildPlan(PlanRequest request)
	{
		var engine = new AllocationEngine(
			request.Inventory,
			request.Locations,
			request.SelectedDecksInOrder,
			request.Options);

		var stepBuilder = new StepBuilder(request);
		var steps = stepBuilder.BuildSteps(engine);

		var deckLookup = request.AllDecks.ToDictionary(d => d.Id);
		var locationLookup = request.Locations.ToDictionary(l => l.Id);

		var selectedContexts = request.SelectedDecksInOrder
			.OrderBy(s => s.OrderIndex)
			.Select(s =>
			{
				var deck = deckLookup[s.DeckId];
				string? boundLocationName = deck.BoundLocationId is not null
					&& locationLookup.TryGetValue(deck.BoundLocationId.Value, out var loc)
					? loc.Name
					: null;

				return new DeckPlanContext(
					DeckId: deck.Id,
					DeckName: deck.Name,
					BoundLocationId: deck.BoundLocationId,
					BoundLocationName: boundLocationName,
					OrderIndex: s.OrderIndex);
			})
			.ToList();

		int totalMoveLines = steps.Sum(s =>
			s.DeckInstructionSets.Sum(d => d.Summary.MoveLineCount));
		int totalCards = steps.Sum(s =>
			s.DeckInstructionSets.Sum(d => d.Summary.TotalCardQuantity));
		int totalMissing = steps.Sum(s =>
			s.DeckInstructionSets.Sum(d => d.Summary.MissingCardCount));

		var summary = new PlanSummary(
			TotalSteps: steps.Count,
			TotalMoveLines: totalMoveLines,
			TotalCardsToMove: totalCards,
			TotalMissingCards: totalMissing);

		return new MovementPlan(
			PlanId: Guid.CreateVersion7(),
			CreatedUtc: request.RequestedUtc,
			SelectedDecksInOrder: selectedContexts,
			Steps: steps,
			Summary: summary);
	}
}