namespace DeckFlow.Data.Repositories;

using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Models;

public class LocationRepository(AppDatabase db) : ILocationRepository
{
	public Task<LocationModel?> GetByIdAsync(Guid id)
		=> db.Connection.FindAsync<LocationModel>(id)!;

	public Task<LocationModel?> GetByNameAsync(string name)
		=> db.Connection.Table<LocationModel>()
			.FirstOrDefaultAsync(l => l.Name == name)!;

	public Task<List<LocationModel>> GetAllAsync()
		=> db.Connection.Table<LocationModel>().ToListAsync();

	public Task InsertAsync(LocationModel location)
		=> db.Connection.InsertAsync(location);

	public Task InsertAllAsync(IEnumerable<LocationModel> locations)
		=> db.Connection.InsertAllAsync(locations);

	public Task UpdateAsync(LocationModel location)
		=> db.Connection.UpdateAsync(location);
}