namespace DeckFlow.Data.Repositories;

using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Models;

public class OwnedCopyRepository(AppDatabase db) : IOwnedCopyRepository
{
	public Task<List<OwnedCopyModel>> GetAllAsync()
		=> db.Connection.Table<OwnedCopyModel>().ToListAsync();

	public Task<List<OwnedCopyModel>> GetByLocationIdAsync(Guid locationId)
		=> db.Connection.Table<OwnedCopyModel>()
			.Where(o => o.LocationId == locationId)
			.ToListAsync();

	public Task<OwnedCopyModel?> GetByLocationAndPrintingAsync(Guid locationId, string scryfallId)
		=> db.Connection.Table<OwnedCopyModel>()
			.FirstOrDefaultAsync(o => o.LocationId == locationId && o.ScryfallId == scryfallId)!;

	public Task InsertAsync(OwnedCopyModel ownedCopy)
		=> db.Connection.InsertAsync(ownedCopy);

	public Task UpdateAsync(OwnedCopyModel ownedCopy)
		=> db.Connection.UpdateAsync(ownedCopy);

	public Task InsertAllAsync(IEnumerable<OwnedCopyModel> ownedCopies)
		=> db.Connection.InsertAllAsync(ownedCopies);

	public Task UpdateAllAsync(IEnumerable<OwnedCopyModel> ownedCopies)
		=> db.Connection.UpdateAllAsync(ownedCopies);

	public async Task DeleteAsync(IEnumerable<OwnedCopyModel> ownedCopies)
	{
		foreach (var ownedCopy in ownedCopies)
			await db.Connection.DeleteAsync(ownedCopy);
	}

	public Task DeleteAllAsync()
		=> db.Connection.DeleteAllAsync<OwnedCopyModel>();

	public async Task<Dictionary<Guid, int>> GetCardCountsByLocationAsync()
	{
		var results = await db.Connection.QueryAsync<LocationCardCount>(
			"SELECT LocationId, SUM(Quantity) AS TotalQuantity FROM OwnedCopies GROUP BY LocationId");
		return results.ToDictionary(r => r.LocationId, r => r.TotalQuantity);
	}

	private class LocationCardCount
	{
		public Guid LocationId { get; set; }
		public int TotalQuantity { get; set; }
	}
}