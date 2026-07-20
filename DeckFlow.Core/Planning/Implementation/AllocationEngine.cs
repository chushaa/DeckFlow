namespace DeckFlow.Core.Planning.Implementation;

using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;

/// <summary>
/// Allocates inventory stacks to deck requirements following priority rules.
/// </summary>
internal sealed class AllocationEngine
{
	private readonly List<AvailableStack> _stacks;
	private readonly Dictionary<LocationId, LocationDefinition> _locationLookup;
	private readonly HashSet<DeckId> _selectedDeckIds;
	private readonly PlanOptions _options;

	public AllocationEngine(
		InventorySnapshot inventory,
		IReadOnlyList<LocationDefinition> locations,
		IReadOnlyList<DeckSelection> selectedDecks,
		PlanOptions options)
	{
		_options = options;
		_locationLookup = locations.ToDictionary(l => l.Id);
		_selectedDeckIds = selectedDecks.Select(s => s.DeckId).ToHashSet();

		_stacks = inventory.Stacks
			.Select(s => new AvailableStack(s.LocationId, s.Card, s.Printing, s.Quantity))
			.ToList();
	}

	/// <summary>
	/// Allocates cards for a single deck's requirements. Returns allocations and missing cards.
	/// </summary>
	public AllocationResult Allocate(DeckDefinition deck)
	{
		var allocations = new List<Allocation>();
		var missing = new List<MissingCardLine>();
		int missingSortKey = 0;

		foreach (var req in deck.Requirements)
		{
			int remaining = req.Quantity;
			var sources = FindSources(req, deck.Id);

			foreach (var source in sources)
			{
				if (remaining <= 0)
					break;

				int take = _options.ConsumeFullyFromSourceBeforeNext
					? Math.Min(remaining, source.Stack.Remaining)
					: Math.Min(remaining, 1);

				if (take <= 0)
					continue;

				source.Stack.Remaining -= take;
				remaining -= take;

				allocations.Add(new Allocation(
					Card: req.Card,
					ActualPrinting: source.Stack.Printing,
					RequestedPrinting: req.RequestedPrinting,
					Quantity: take,
					SourceLocationId: source.Stack.LocationId,
					Reason: source.Reason));
			}

			if (remaining > 0 && _options.IncludeMissingCardsSection)
			{
				missing.Add(new MissingCardLine(
					Card: req.Card,
					RequestedPrinting: req.RequestedPrinting,
					QuantityNeeded: remaining,
					SortKey: missingSortKey++));
			}
		}

		if (_options.SortCardsAlphabetically)
		{
			allocations.Sort((a, b) =>
				string.Compare(a.Card.Name, b.Card.Name, StringComparison.OrdinalIgnoreCase));
			missing.Sort((a, b) =>
				string.Compare(a.Card.Name, b.Card.Name, StringComparison.OrdinalIgnoreCase));

			for (int i = 0; i < missing.Count; i++)
				missing[i] = missing[i] with { SortKey = i };
		}

		return new AllocationResult(allocations, missing);
	}

	/// <summary>
	/// Restores quantity to the available pool (used when returning cards between steps).
	/// </summary>
	public void ReturnToPool(LocationId locationId, CardId cardId, PrintingRef printing, int quantity)
	{
		var stack = _stacks.Find(s =>
			s.LocationId == locationId
			&& s.Card.Id == cardId
			&& s.Printing == printing);

		if (stack is not null)
			stack.Remaining += quantity;
		else
			_stacks.Add(new AvailableStack(locationId, new CardRef(cardId, ""), printing, quantity));
	}

	private List<RankedSource> FindSources(DeckRequirement req, DeckId requestingDeckId)
	{
		var candidates = new List<RankedSource>();

		foreach (var stack in _stacks)
		{
			if (stack.Remaining <= 0)
				continue;

			if (stack.Card.Id != req.Card.Id)
				continue;

			if (!_locationLookup.TryGetValue(stack.LocationId, out var location))
				continue;

			bool isExactPrintingMatch = req.RequestedPrinting is not null
				&& stack.Printing.Id is not null
				&& req.RequestedPrinting.Id == stack.Printing.Id;

			var reason = DetermineReason(location, requestingDeckId, isExactPrintingMatch);
			int priority = GetPriority(reason, isExactPrintingMatch);

			candidates.Add(new RankedSource(stack, location, reason, priority));
		}

		candidates.Sort((a, b) =>
		{
			int cmp = a.Priority.CompareTo(b.Priority);
			if (cmp != 0)
				return cmp;

			cmp = string.Compare(a.Location.Name, b.Location.Name, StringComparison.OrdinalIgnoreCase);
			if (cmp != 0)
				return cmp;

			cmp = b.Stack.Remaining.CompareTo(a.Stack.Remaining);
			return cmp;
		});

		return candidates;
	}

	private AllocationReason DetermineReason(
		LocationDefinition location,
		DeckId requestingDeckId,
		bool isExactPrintingMatch)
	{
		if (isExactPrintingMatch && _options.PrintingMatchOverridesLocationPriority)
			return AllocationReason.ExactPrintingMatch;

		if (location.Kind == SourceLocationKind.DeckBound)
		{
			if (location.BoundDeckId is not null && _selectedDeckIds.Contains(location.BoundDeckId.Value))
			{
				if (location.BoundDeckId.Value == requestingDeckId)
					return AllocationReason.FromUnselectedDeck;
				return AllocationReason.FromSelectedDeck;
			}
			return AllocationReason.FromUnselectedDeck;
		}

		return AllocationReason.FromUnboundCollection;
	}

	private static int GetPriority(AllocationReason reason, bool isExactPrintingMatch)
	{
		if (isExactPrintingMatch)
			return 0;

		return reason switch
		{
			AllocationReason.ExactPrintingMatch => 0,
			AllocationReason.FromUnselectedDeck => 1,
			AllocationReason.FromUnboundCollection => 2,
			AllocationReason.FromSelectedDeck => 3,
			_ => 99
		};
	}
}

internal sealed class AvailableStack(LocationId locationId, CardRef card, PrintingRef printing, int quantity)
{
	public LocationId LocationId { get; } = locationId;
	public CardRef Card { get; } = card;
	public PrintingRef Printing { get; } = printing;
	public int Remaining { get; set; } = quantity;
}

internal sealed record Allocation(
	CardRef Card,
	PrintingRef ActualPrinting,
	PrintingRef? RequestedPrinting,
	int Quantity,
	LocationId SourceLocationId,
	AllocationReason Reason);

internal sealed record AllocationResult(
	List<Allocation> Allocations,
	List<MissingCardLine> Missing);

internal sealed record RankedSource(
	AvailableStack Stack,
	LocationDefinition Location,
	AllocationReason Reason,
	int Priority);