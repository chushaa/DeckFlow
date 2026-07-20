namespace DeckFlow.ViewModels;

public class DeckTileViewModel(Guid deckId, string name, int cardCount, string boundLocationName)
{
	public Guid DeckId { get; } = deckId;
	public string Name { get; } = name;
	public int CardCount { get; } = cardCount;
	public string BoundLocationName { get; } = boundLocationName;
	public string CardCountText => $"{CardCount} cards";
}