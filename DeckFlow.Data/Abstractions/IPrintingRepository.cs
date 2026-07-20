namespace DeckFlow.Data.Abstractions;

using DeckFlow.Data.Models;

public interface IPrintingRepository
{
	Task<PrintingModel?> GetByScryfallIdAsync(string scryfallId);
	Task<List<PrintingModel>> GetByCardIdAsync(Guid cardId);
	Task<List<PrintingModel>> GetAllAsync();
	Task InsertAsync(PrintingModel printing);
	Task InsertAllAsync(IEnumerable<PrintingModel> printings);
}