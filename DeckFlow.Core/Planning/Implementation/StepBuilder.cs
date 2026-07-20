namespace DeckFlow.Core.Planning.Implementation;

using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;

/// <summary>
/// Builds the sequence of plan steps (PreGame, Transitions, FinalReturn) by tracking
/// where each allocated card physically resides across steps.
/// </summary>
internal sealed class StepBuilder
{
	private readonly PlanRequest _request;
	private readonly Dictionary<DeckId, DeckDefinition> _deckLookup;
	private readonly Dictionary<LocationId, LocationDefinition> _locationLookup;
	private readonly InstructionBuilder _instructionBuilder;

	// Tracks where each borrowed card currently is: (card, printing, originalLocation) → currentDeckId
	private readonly List<BorrowedCard> _borrowedCards = [];

	public StepBuilder(PlanRequest request)
	{
		_request = request;
		_deckLookup = request.AllDecks.ToDictionary(d => d.Id);
		_locationLookup = request.Locations.ToDictionary(l => l.Id);
		_instructionBuilder = new InstructionBuilder(request.Options, _locationLookup);
	}

	public List<PlanStep> BuildSteps(AllocationEngine engine)
	{
		var steps = new List<PlanStep>();
		var selectedDecks = _request.SelectedDecksInOrder
			.OrderBy(s => s.OrderIndex)
			.ToList();

		if (selectedDecks.Count == 0)
			return steps;

		// Step 0: PreGame — assemble first deck
		var firstSelection = selectedDecks[0];
		var firstDeck = _deckLookup[firstSelection.DeckId];
		var preGameStep = BuildPreGameStep(engine, firstDeck, steps.Count);
		steps.Add(preGameStep);

		// Steps 1..N-1: Transitions between consecutive decks
		for (int i = 1; i < selectedDecks.Count; i++)
		{
			var fromDeck = _deckLookup[selectedDecks[i - 1].DeckId];
			var toDeck = _deckLookup[selectedDecks[i].DeckId];
			var transitionStep = BuildTransitionStep(engine, fromDeck, toDeck, steps.Count);
			steps.Add(transitionStep);
		}

		// Final step: Return all remaining borrowed cards
		var lastDeck = _deckLookup[selectedDecks[^1].DeckId];
		var finalStep = BuildFinalReturnStep(lastDeck, steps.Count);
		steps.Add(finalStep);

		return steps;
	}

	private PreGameStep BuildPreGameStep(AllocationEngine engine, DeckDefinition deck, int stepIndex)
	{
		var result = engine.Allocate(deck);
		var notes = new List<PlannerNote>();

		// Track all pulled cards as borrowed
		foreach (var alloc in result.Allocations)
		{
			_borrowedCards.Add(new BorrowedCard(
				Card: alloc.Card,
				Printing: alloc.ActualPrinting,
				OriginalLocationId: alloc.SourceLocationId,
				Quantity: alloc.Quantity,
				CurrentDeckId: deck.Id));
		}

		// Build move lines as "Pull" actions
		var moveLines = BuildPullMoveLines(result.Allocations, deck);

		var instructionSet = _instructionBuilder.Build(
			deck.Id,
			deck.Name,
			moveLines,
			result.Missing);

		string deckName = deck.Name;
		return new PreGameStep(
			StepIndex: stepIndex,
			Title: $"Game 1: {deckName}",
			Subtitle: "Pull these cards before playing.",
			TargetDeckId: deck.Id,
			TargetDeckName: deckName,
			DeckInstructionSets: [instructionSet],
			Notes: notes);
	}

	private TransitionStep BuildTransitionStep(
		AllocationEngine engine,
		DeckDefinition fromDeck,
		DeckDefinition toDeck,
		int stepIndex)
	{
		var notes = new List<PlannerNote>();
		var allMoveLines = new List<MoveLine>();

		// Step 1: Return ALL borrowed cards from the previous deck to the pool.
		// This lets the allocation engine naturally re-discover carried-over cards.
		var fromDeckBorrowed = _borrowedCards.Where(b => b.CurrentDeckId == fromDeck.Id).ToList();
		foreach (var borrowed in fromDeckBorrowed)
		{
			engine.ReturnToPool(
				borrowed.OriginalLocationId,
				borrowed.Card.Id,
				borrowed.Printing,
				borrowed.Quantity);
		}
		_borrowedCards.RemoveAll(b => b.CurrentDeckId == fromDeck.Id);

		// Step 2: Allocate for the next deck (carried-over cards are now back in the pool)
		var toDeckResult = engine.Allocate(toDeck);

		// Step 3: Determine which allocations re-use previously borrowed cards (carried over)
		// vs which are truly new pulls from the pool
		var previouslyBorrowedLookup = new Dictionary<(CardId, LocationId), BorrowedCard>();
		foreach (var borrowed in fromDeckBorrowed)
			previouslyBorrowedLookup.TryAdd((borrowed.Card.Id, borrowed.OriginalLocationId), borrowed);

		var reAllocatedKeys = new HashSet<(CardId, LocationId)>();
		foreach (var alloc in toDeckResult.Allocations)
		{
			var key = (alloc.Card.Id, alloc.SourceLocationId);
			if (previouslyBorrowedLookup.ContainsKey(key))
				reAllocatedKeys.Add(key);
		}

		// Step 4: Build return move lines for cards NOT re-allocated by the next deck
		foreach (var borrowed in fromDeckBorrowed)
		{
			var key = (borrowed.Card.Id, borrowed.OriginalLocationId);
			if (reAllocatedKeys.Contains(key))
				continue;

			// Skip self-sourced cards — their home IS the previous deck's box, they never physically moved
			if (fromDeck.BoundLocationId is not null && borrowed.OriginalLocationId == fromDeck.BoundLocationId)
				continue;

			string sourceName = _deckLookup.TryGetValue(fromDeck.Id, out var fd) ? fd.Name : "Unknown";
			string destName = _locationLookup.TryGetValue(borrowed.OriginalLocationId, out var loc) ? loc.Name : "Unknown";

			allMoveLines.Add(new MoveLine(
				Card: borrowed.Card,
				ActualPrinting: borrowed.Printing,
				RequestedPrinting: null,
				Quantity: borrowed.Quantity,
				Action: MoveAction.Return,
				SourceLocationId: fromDeck.BoundLocationId ?? new LocationId(Guid.Empty),
				SourceLocationName: sourceName,
				DestinationDeckId: new DeckId(Guid.Empty),
				DestinationDeckName: destName,
				DestinationLocationId: borrowed.OriginalLocationId,
				Reason: AllocationReason.FromSelectedDeck,
				SortKey: 0));
		}

		// Step 5: Build pull move lines for carry-over cards (physically in fromDeck's box)
		if (fromDeck.BoundLocationId is not null)
		{
			string carryOverSourceName = _locationLookup.TryGetValue(fromDeck.BoundLocationId.Value, out var fromLoc)
				? fromLoc.Name
				: fromDeck.Name;

			foreach (var alloc in toDeckResult.Allocations)
			{
				var key = (alloc.Card.Id, alloc.SourceLocationId);
				if (!reAllocatedKeys.Contains(key))
					continue;

				// Skip if both decks share the same bound location (card is already in the right box)
				if (toDeck.BoundLocationId is not null && fromDeck.BoundLocationId == toDeck.BoundLocationId)
					continue;

				allMoveLines.Add(new MoveLine(
					Card: alloc.Card,
					ActualPrinting: alloc.ActualPrinting,
					RequestedPrinting: alloc.RequestedPrinting,
					Quantity: alloc.Quantity,
					Action: MoveAction.Pull,
					SourceLocationId: fromDeck.BoundLocationId.Value,
					SourceLocationName: carryOverSourceName,
					DestinationDeckId: toDeck.Id,
					DestinationDeckName: toDeck.Name,
					DestinationLocationId: toDeck.BoundLocationId,
					Reason: alloc.Reason,
					SortKey: 0));
			}
		}

		// Step 6: Build pull move lines for newly allocated cards (not carried over)
		var newAllocations = toDeckResult.Allocations
			.Where(a => !previouslyBorrowedLookup.ContainsKey((a.Card.Id, a.SourceLocationId)))
			.ToList();
		var pullLines = BuildPullMoveLines(newAllocations, toDeck);
		allMoveLines.AddRange(pullLines);

		// Step 7: Track all allocated cards as borrowed for toDeck
		foreach (var alloc in toDeckResult.Allocations)
		{
			_borrowedCards.Add(new BorrowedCard(
				Card: alloc.Card,
				Printing: alloc.ActualPrinting,
				OriginalLocationId: alloc.SourceLocationId,
				Quantity: alloc.Quantity,
				CurrentDeckId: toDeck.Id));
		}

		var instructionSet = _instructionBuilder.Build(
			toDeck.Id,
			toDeck.Name,
			allMoveLines,
			toDeckResult.Missing);

		return new TransitionStep(
			StepIndex: stepIndex,
			Title: $"Next Game: {fromDeck.Name} → {toDeck.Name}",
			Subtitle: "Move these cards to play the next deck.",
			FromDeckId: fromDeck.Id,
			FromDeckName: fromDeck.Name,
			ToDeckId: toDeck.Id,
			ToDeckName: toDeck.Name,
			DeckInstructionSets: [instructionSet],
			Notes: notes);
	}

	private FinalReturnStep BuildFinalReturnStep(DeckDefinition lastDeck, int stepIndex)
	{
		var notes = new List<PlannerNote>();
		var moveLines = new List<MoveLine>();

		foreach (var borrowed in _borrowedCards)
		{
			// Skip cards that are already at their home location
			var currentDeckBoundLocation = _deckLookup.TryGetValue(borrowed.CurrentDeckId, out var currentDeck)
				? currentDeck.BoundLocationId
				: null;

			if (currentDeckBoundLocation is not null && currentDeckBoundLocation == borrowed.OriginalLocationId)
				continue;

			string sourceName = _deckLookup.TryGetValue(borrowed.CurrentDeckId, out var d) ? d.Name : "Unknown";
			string destName = _locationLookup.TryGetValue(borrowed.OriginalLocationId, out var loc) ? loc.Name : "Unknown";

			moveLines.Add(new MoveLine(
				Card: borrowed.Card,
				ActualPrinting: borrowed.Printing,
				RequestedPrinting: null,
				Quantity: borrowed.Quantity,
				Action: MoveAction.Return,
				SourceLocationId: currentDeck?.BoundLocationId ?? new LocationId(Guid.Empty),
				SourceLocationName: sourceName,
				DestinationDeckId: new DeckId(Guid.Empty),
				DestinationDeckName: destName,
				DestinationLocationId: borrowed.OriginalLocationId,
				Reason: AllocationReason.FromSelectedDeck,
				SortKey: 0));
		}

		var instructionSet = _instructionBuilder.Build(
			lastDeck.Id,
			lastDeck.Name,
			moveLines,
			[]);

		_borrowedCards.Clear();

		return new FinalReturnStep(
			StepIndex: stepIndex,
			Title: "Finish: Return Cards",
			Subtitle: "Return remaining cards to their original locations.",
			DeckInstructionSets: [instructionSet],
			Notes: notes);
	}

	private List<MoveLine> BuildPullMoveLines(List<Allocation> allocations, DeckDefinition deck)
	{
		var lines = new List<MoveLine>();
		foreach (var alloc in allocations)
		{
			// Skip cards sourced from the deck's own bound location — they are already in the box
			if (deck.BoundLocationId is not null && alloc.SourceLocationId == deck.BoundLocationId)
				continue;

			string sourceName = _locationLookup.TryGetValue(alloc.SourceLocationId, out var loc)
				? loc.Name
				: "Unknown";

			lines.Add(new MoveLine(
				Card: alloc.Card,
				ActualPrinting: alloc.ActualPrinting,
				RequestedPrinting: alloc.RequestedPrinting,
				Quantity: alloc.Quantity,
				Action: MoveAction.Pull,
				SourceLocationId: alloc.SourceLocationId,
				SourceLocationName: sourceName,
				DestinationDeckId: deck.Id,
				DestinationDeckName: deck.Name,
				DestinationLocationId: deck.BoundLocationId,
				Reason: alloc.Reason,
				SortKey: 0));
		}
		return lines;
	}

}

internal sealed record BorrowedCard(
	CardRef Card,
	PrintingRef Printing,
	LocationId OriginalLocationId,
	int Quantity,
	DeckId CurrentDeckId);