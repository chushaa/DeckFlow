namespace DeckFlow.Data.Abstractions;

using DeckFlow.Data.Models;

public interface IDeckRequirementRepository
{
	Task<List<DeckRequirementModel>> GetByDeckIdAsync(Guid deckId);
	Task InsertAllAsync(IEnumerable<DeckRequirementModel> requirements);
	Task DeleteByDeckIdAsync(Guid deckId);
}