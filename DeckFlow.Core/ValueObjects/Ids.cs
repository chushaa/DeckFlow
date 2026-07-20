namespace DeckFlow.Core.ValueObjects;

public readonly record struct DeckId(Guid Value)
{
	public static DeckId New() => new(Guid.CreateVersion7());
	public override string ToString() => Value.ToString();
}

public readonly record struct LocationId(Guid Value)
{
	public static LocationId New() => new(Guid.CreateVersion7());
	public override string ToString() => Value.ToString();
}

public readonly record struct CardId(Guid Value)
{
	public static CardId New() => new(Guid.CreateVersion7());
	public override string ToString() => Value.ToString();
}

// PrintingId uses string because Scryfall IDs are UUIDs stored as strings
public readonly record struct PrintingId(string Value)
{
	public override string ToString() => Value;
}