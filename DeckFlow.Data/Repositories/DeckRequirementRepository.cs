namespace DeckFlow.Data.Repositories;

using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Models;

public class DeckRequirementRepository(AppDatabase db) : IDeckRequirementRepository
{
	public Task<List<DeckRequirementModel>> GetByDeckIdAsync(Guid deckId)
		=> db.Connection.Table<DeckRequirementModel>()
			.Where(r => r.DeckId == deckId)
			.ToListAsync();

	public Task InsertAllAsync(IEnumerable<DeckRequirementModel> requirements)
		=> db.Connection.InsertAllAsync(requirements);

	public async Task DeleteByDeckIdAsync(Guid deckId)
	{
		var existing = await GetByDeckIdAsync(deckId);
		foreach (var req in existing)
			await db.Connection.DeleteAsync(req);
	}
}