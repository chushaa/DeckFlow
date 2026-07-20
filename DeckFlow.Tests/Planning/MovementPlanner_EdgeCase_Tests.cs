using DeckFlow.Core.Planning;
using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;
using DeckFlow.Tests.Helpers;

namespace DeckFlow.Tests.Planning;

public sealed class MovementPlanner_EdgeCase_Tests
{
	private readonly IMovementPlanner _planner = new MovementPlanner();

	[Fact]
	public void AllCardsMissing_OnlyMissingCardSection_NoMoveLines()
	{
		var deckId = DeckId.New();
		var locId = LocationId.New();
		var card = TestData.Card("Lightning Bolt");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Burn", locId, [new DeckRequirement(card, 4, null)])
			],
			Locations: [new LocationDefinition(locId, "Burn Box", SourceLocationKind.DeckBound, deckId)],
			Inventory: new InventorySnapshot([]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		Assert.Equal(2, plan.Steps.Count);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);

		// No source groups (no cards to pull)
		Assert.Empty(deckSet.SourceGroups);

		// Missing section should have Lightning Bolt x4
		Assert.NotNull(deckSet.MissingCards);
		var missing = Assert.Single(deckSet.MissingCards.Cards);
		Assert.Equal("Lightning Bolt", missing.Card.Name);
		Assert.Equal(4, missing.QuantityNeeded);

		Assert.Equal(4, plan.Summary.TotalMissingCards);
	}

	[Fact]
	public void PartialOwnership_SplitBetweenMovesAndMissing()
	{
		var deckId = DeckId.New();
		var locId = LocationId.New();
		var binderId = LocationId.New();
		var card = TestData.Card("Counterspell");
		var printing = TestData.Printing("scryfall-cs-1");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Control", locId, [new DeckRequirement(card, 4, null)])
			],
			Locations:
			[
				new LocationDefinition(locId, "Control Box", SourceLocationKind.DeckBound, deckId),
				new LocationDefinition(binderId, "Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot([new InventoryStack(binderId, card, printing, 2)]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);

		// Should pull 2 from Binder
		var group = Assert.Single(deckSet.SourceGroups);
		Assert.Equal("Binder", group.LocationName);
		var move = Assert.Single(group.MoveLines);
		Assert.Equal(2, move.Quantity);

		// Should report 2 missing
		Assert.NotNull(deckSet.MissingCards);
		var missing = Assert.Single(deckSet.MissingCards.Cards);
		Assert.Equal(2, missing.QuantityNeeded);
	}

	[Fact]
	public void ExactPrintingMatch_OverridesLocationPriority()
	{
		var deckId = DeckId.New();
		var deckLocId = LocationId.New();
		var binderId = LocationId.New();
		var farBinderId = LocationId.New();

		var card = TestData.Card("Sol Ring");
		var requestedPrinting = TestData.Printing("scryfall-sol-special", "SPE", "1");
		var genericPrinting = TestData.Printing("scryfall-sol-generic", "C21", "100");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Commander", deckLocId,
				[
					new DeckRequirement(card, 1, requestedPrinting)
				])
			],
			Locations:
			[
				new LocationDefinition(deckLocId, "Commander Box", SourceLocationKind.DeckBound, deckId),
				new LocationDefinition(binderId, "Close Binder", SourceLocationKind.UnboundCollection, null),
				new LocationDefinition(farBinderId, "Far Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(binderId, card, genericPrinting, 1),
				new InventoryStack(farBinderId, card, requestedPrinting, 1)
			]),
			Options: new PlanOptions(PrintingMatchOverridesLocationPriority: true));

		var plan = _planner.BuildPlan(request);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);
		var group = Assert.Single(deckSet.SourceGroups);

		// Should pull from Far Binder (exact printing match) not Close Binder
		Assert.Equal("Far Binder", group.LocationName);
		var move = Assert.Single(group.MoveLines);
		Assert.Equal(AllocationReason.ExactPrintingMatch, move.Reason);
	}

	[Fact]
	public void EmptyDeck_ZeroRequirements_NoInstructions()
	{
		var deckId = DeckId.New();
		var locId = LocationId.New();

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Empty Deck", locId, [])
			],
			Locations: [new LocationDefinition(locId, "Box", SourceLocationKind.DeckBound, deckId)],
			Inventory: new InventorySnapshot([]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		Assert.Equal(2, plan.Steps.Count);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);
		Assert.Empty(deckSet.SourceGroups);
		Assert.Null(deckSet.MissingCards);

		Assert.Equal(0, plan.Summary.TotalMoveLines);
		Assert.Equal(0, plan.Summary.TotalCardsToMove);
		Assert.Equal(0, plan.Summary.TotalMissingCards);
	}

	[Fact]
	public void SingleDeck_NoTransitions_OnlyPreGameAndFinalReturn()
	{
		var (req, _) = TestData.BuildScenario1();

		var plan = _planner.BuildPlan(req);

		Assert.Equal(2, plan.Steps.Count);
		Assert.IsType<PreGameStep>(plan.Steps[0]);
		Assert.IsType<FinalReturnStep>(plan.Steps[1]);
	}

	[Fact]
	public void NoSelectedDecks_EmptyPlan()
	{
		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [],
			AllDecks: [],
			Locations: [],
			Inventory: new InventorySnapshot([]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		Assert.Empty(plan.Steps);
		Assert.Equal(0, plan.Summary.TotalSteps);
	}

	[Fact]
	public void MissingCardsSection_Disabled_NoMissingReported()
	{
		var deckId = DeckId.New();
		var locId = LocationId.New();
		var card = TestData.Card("Black Lotus");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Power", locId, [new DeckRequirement(card, 1, null)])
			],
			Locations: [new LocationDefinition(locId, "Vault", SourceLocationKind.DeckBound, deckId)],
			Inventory: new InventorySnapshot([]),
			Options: new PlanOptions(IncludeMissingCardsSection: false));

		var plan = _planner.BuildPlan(request);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);
		Assert.Null(deckSet.MissingCards);
	}

	[Fact]
	public void SourceGroups_SortedAlphabetically()
	{
		var deckId = DeckId.New();
		var deckLocId = LocationId.New();
		var zBinderId = LocationId.New();
		var aBinderId = LocationId.New();

		var card1 = TestData.Card("Alpha Card");
		var card2 = TestData.Card("Zeta Card");
		var print1 = TestData.Printing("scryfall-1");
		var print2 = TestData.Printing("scryfall-2");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Test", deckLocId,
				[
					new DeckRequirement(card1, 1, null),
					new DeckRequirement(card2, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(deckLocId, "Test Box", SourceLocationKind.DeckBound, deckId),
				new LocationDefinition(zBinderId, "Zebra Binder", SourceLocationKind.UnboundCollection, null),
				new LocationDefinition(aBinderId, "Alpha Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(zBinderId, card1, print1, 1),
				new InventoryStack(aBinderId, card2, print2, 1)
			]),
			Options: new PlanOptions(SortLocationGroupsAlphabetically: true));

		var plan = _planner.BuildPlan(request);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);

		Assert.Equal(2, deckSet.SourceGroups.Count);
		Assert.Equal("Alpha Binder", deckSet.SourceGroups[0].LocationName);
		Assert.Equal("Zebra Binder", deckSet.SourceGroups[1].LocationName);
	}

	[Fact]
	public void CardsWithinGroup_SortedAlphabetically()
	{
		var deckId = DeckId.New();
		var deckLocId = LocationId.New();
		var binderId = LocationId.New();

		var cardZ = TestData.Card("Zombify");
		var cardA = TestData.Card("Absorb");
		var printZ = TestData.Printing("scryfall-z");
		var printA = TestData.Printing("scryfall-a");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Test", deckLocId,
				[
					new DeckRequirement(cardZ, 1, null),
					new DeckRequirement(cardA, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(deckLocId, "Test Box", SourceLocationKind.DeckBound, deckId),
				new LocationDefinition(binderId, "Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(binderId, cardZ, printZ, 1),
				new InventoryStack(binderId, cardA, printA, 1)
			]),
			Options: new PlanOptions(SortCardsAlphabetically: true));

		var plan = _planner.BuildPlan(request);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);
		var group = Assert.Single(deckSet.SourceGroups);

		Assert.Equal(2, group.MoveLines.Count);
		Assert.Equal("Absorb", group.MoveLines[0].Card.Name);
		Assert.Equal("Zombify", group.MoveLines[1].Card.Name);
	}

	[Fact]
	public void SortKeys_AreSequential()
	{
		var (req, _) = TestData.BuildGuardrail_ConsumeFully();

		var plan = _planner.BuildPlan(req);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);

		// Group SortKeys should be sequential
		for (int i = 0; i < deckSet.SourceGroups.Count; i++)
			Assert.Equal(i, deckSet.SourceGroups[i].SortKey);

		// MoveLine SortKeys within each group should be sequential
		foreach (var group in deckSet.SourceGroups)
		{
			for (int i = 0; i < group.MoveLines.Count; i++)
				Assert.Equal(i, group.MoveLines[i].SortKey);
		}
	}
}