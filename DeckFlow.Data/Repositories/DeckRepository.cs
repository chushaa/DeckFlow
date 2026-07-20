namespace DeckFlow.Data.Repositories;

using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Models;

public class DeckRepository(AppDatabase db) : IDeckRepository
{
	public Task<DeckModel?> GetByIdAsync(Guid id)
		=> db.Connection.FindAsync<DeckModel>(id)!;

	public Task<List<DeckModel>> GetAllAsync()
		=> db.Connection.Table<DeckModel>().ToListAsync();

	public Task InsertAsync(DeckModel deck)
		=> db.Connection.InsertAsync(deck);

	public Task UpdateAsync(DeckModel deck)
		=> db.Connection.UpdateAsync(deck);

	public async Task DeleteAsync(Guid id)
	{
		var deck = await db.Connection.FindAsync<DeckModel>(id);
		if (deck is not null)
			await db.Connection.DeleteAsync(deck);
	}
}