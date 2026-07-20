namespace DeckFlow.Data.Services;

using DeckFlow.Core.Planning.Contracts;

public interface IInventoryService
{
	Task<PlanRequest> BuildPlanRequestAsync(IReadOnlyList<DeckSelection> selectedDecks);
}