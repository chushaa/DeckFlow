namespace DeckFlow.Data.Repositories;

using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Models;

public class PrintingRepository(AppDatabase db) : IPrintingRepository
{
	public Task<PrintingModel?> GetByScryfallIdAsync(string scryfallId)
		=> db.Connection.FindAsync<PrintingModel>(scryfallId)!;

	public Task<List<PrintingModel>> GetByCardIdAsync(Guid cardId)
		=> db.Connection.Table<PrintingModel>()
			.Where(p => p.CardId == cardId)
			.ToListAsync();

	public Task<List<PrintingModel>> GetAllAsync()
		=> db.Connection.Table<PrintingModel>().ToListAsync();

	public Task InsertAsync(PrintingModel printing)
		=> db.Connection.InsertAsync(printing);

	public Task InsertAllAsync(IEnumerable<PrintingModel> printings)
		=> db.Connection.InsertAllAsync(printings);
}