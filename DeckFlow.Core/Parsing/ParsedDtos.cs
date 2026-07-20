namespace DeckFlow.Core.Parsing;

public sealed record ParsedDeckEntry(
	int Quantity,
	string CardName,
	string? BackFaceName,
	string? SetCode,
	string? CollectorNumber,
	bool IsFoil,
	bool IsEtched);

public sealed record ParsedCollectionEntry(
	string LocationName,
	string CardName,
	string? BackFaceName,
	string SetCode,
	string SetName,
	string CollectorNumber,
	bool IsFoil,
	int Quantity,
	string ScryfallId);