namespace DeckFlow.Data.Repositories;

using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Models;

public class CardRepository(AppDatabase db) : ICardRepository
{
	public Task<CardModel?> GetByIdAsync(Guid id)
		=> db.Connection.FindAsync<CardModel>(id)!;

	public Task<CardModel?> GetByNameAsync(string name)
		=> db.Connection.Table<CardModel>()
			.FirstOrDefaultAsync(c => c.Name == name)!;

	public Task<List<CardModel>> GetAllAsync()
		=> db.Connection.Table<CardModel>().ToListAsync();

	public Task InsertAsync(CardModel card)
		=> db.Connection.InsertAsync(card);

	public Task InsertAllAsync(IEnumerable<CardModel> cards)
		=> db.Connection.InsertAllAsync(cards);

	public Task UpdateAsync(CardModel card)
		=> db.Connection.UpdateAsync(card);

	public Task UpdateAllAsync(IEnumerable<CardModel> cards)
		=> db.Connection.UpdateAllAsync(cards);
}