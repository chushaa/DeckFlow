namespace DeckFlow.Data.Abstractions;

using DeckFlow.Data.Models;

public interface ICardRepository
{
	Task<CardModel?> GetByIdAsync(Guid id);
	Task<CardModel?> GetByNameAsync(string name);
	Task<List<CardModel>> GetAllAsync();
	Task InsertAsync(CardModel card);
	Task InsertAllAsync(IEnumerable<CardModel> cards);
	Task UpdateAsync(CardModel card);
	Task UpdateAllAsync(IEnumerable<CardModel> cards);
}