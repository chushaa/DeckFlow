namespace DeckFlow.ViewModels;

public class BinderTileViewModel(Guid locationId, string name, string color, int cardCount, string? assignedDeckNames)
{
	public Guid LocationId { get; } = locationId;
	public string Name { get; } = name;
	public string Color { get; } = color;
	public int CardCount { get; } = cardCount;
	public string CardCountText => $"{CardCount} cards";
	public string? AssignedDeckNames { get; } = assignedDeckNames;
	public bool HasAssignedDecks => !string.IsNullOrEmpty(AssignedDeckNames);
}