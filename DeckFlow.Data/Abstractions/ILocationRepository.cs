namespace DeckFlow.Data.Abstractions;

using DeckFlow.Data.Models;

public interface ILocationRepository
{
	Task<LocationModel?> GetByIdAsync(Guid id);
	Task<LocationModel?> GetByNameAsync(string name);
	Task<List<LocationModel>> GetAllAsync();
	Task InsertAsync(LocationModel location);
	Task InsertAllAsync(IEnumerable<LocationModel> locations);
	Task UpdateAsync(LocationModel location);
}