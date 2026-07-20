namespace DeckFlow.Core.Planning.Implementation;

using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;

/// <summary>
/// Groups move lines by source location, sorts them, and assigns stable SortKeys
/// for deterministic UI rendering.
/// </summary>
internal sealed class InstructionBuilder
{
	private readonly PlanOptions _options;
	private readonly Dictionary<LocationId, LocationDefinition> _locationLookup;

	public InstructionBuilder(PlanOptions options, Dictionary<LocationId, LocationDefinition> locationLookup)
	{
		_options = options;
		_locationLookup = locationLookup;
	}

	public DeckInstructionSet Build(
		DeckId deckId,
		string deckName,
		List<MoveLine> moveLines,
		List<MissingCardLine> missing)
	{
		// Group move lines by source location
		var groups = moveLines
			.GroupBy(m => m.SourceLocationId)
			.Select(g =>
			{
				var locationId = g.Key;
				string locationName = g.First().SourceLocationName;
				var locationKind = _locationLookup.TryGetValue(locationId, out var loc)
					? loc.Kind
					: SourceLocationKind.Unknown;

				var sortedLines = g.ToList();
				if (_options.SortCardsAlphabetically)
				{
					sortedLines.Sort((a, b) =>
						string.Compare(a.Card.Name, b.Card.Name, StringComparison.OrdinalIgnoreCase));
				}

				// Assign SortKeys within group
				for (int i = 0; i < sortedLines.Count; i++)
					sortedLines[i] = sortedLines[i] with { SortKey = i };

				return new SourceLocationGroup(
					LocationId: locationId,
					LocationName: locationName,
					LocationKind: locationKind,
					MoveLines: sortedLines,
					SortKey: 0);
			})
			.ToList();

		// Sort groups alphabetically by location name
		if (_options.SortLocationGroupsAlphabetically)
		{
			groups.Sort((a, b) =>
				string.Compare(a.LocationName, b.LocationName, StringComparison.OrdinalIgnoreCase));
		}

		// Assign group SortKeys
		for (int i = 0; i < groups.Count; i++)
			groups[i] = groups[i] with { SortKey = i };

		var missingSection = missing.Count > 0
			? new MissingCardSection(missing)
			: null;

		int totalMoveLines = groups.Sum(g => g.MoveLines.Count);
		int totalQuantity = groups.Sum(g => g.MoveLines.Sum(m => m.Quantity));
		int missingCount = missing.Sum(m => m.QuantityNeeded);

		var summary = new DeckInstructionSummary(
			MoveLineCount: totalMoveLines,
			TotalCardQuantity: totalQuantity,
			MissingCardCount: missingCount);

		return new DeckInstructionSet(
			DeckId: deckId,
			DeckName: deckName,
			SourceGroups: groups,
			MissingCards: missingSection,
			Summary: summary);
	}
}