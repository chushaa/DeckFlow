namespace DeckFlow.Data.Abstractions;

using DeckFlow.Data.Models;

public interface IDeckRepository
{
	Task<DeckModel?> GetByIdAsync(Guid id);
	Task<List<DeckModel>> GetAllAsync();
	Task InsertAsync(DeckModel deck);
	Task UpdateAsync(DeckModel deck);
	Task DeleteAsync(Guid id);
}