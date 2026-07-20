namespace DeckFlow.Core.ValueObjects;

public sealed record CardRef(CardId Id, string Name);

public sealed record PrintingRef(
	PrintingId? Id,
	string? SetCode,
	string? CollectorNumber,
	bool IsFoil);