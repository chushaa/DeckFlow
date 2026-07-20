namespace DeckFlow.Data.Abstractions;

using DeckFlow.Data.Models;

public interface IOwnedCopyRepository
{
	Task<List<OwnedCopyModel>> GetAllAsync();
	Task<List<OwnedCopyModel>> GetByLocationIdAsync(Guid locationId);
	Task<OwnedCopyModel?> GetByLocationAndPrintingAsync(Guid locationId, string scryfallId);
	Task InsertAsync(OwnedCopyModel ownedCopy);
	Task UpdateAsync(OwnedCopyModel ownedCopy);
	Task InsertAllAsync(IEnumerable<OwnedCopyModel> ownedCopies);
	Task UpdateAllAsync(IEnumerable<OwnedCopyModel> ownedCopies);
	Task DeleteAsync(IEnumerable<OwnedCopyModel> ownedCopies);
	Task DeleteAllAsync();
	Task<Dictionary<Guid, int>> GetCardCountsByLocationAsync();
}