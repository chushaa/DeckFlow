namespace DeckFlow.Data.Services;

public interface IDeckImportService
{
	Task<Guid> ImportAsync(string decklistText, string deckName, Guid? boundLocationId);
	Task ReplaceRequirementsAsync(Guid deckId, string decklistText);
}