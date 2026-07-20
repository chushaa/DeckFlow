using DeckFlow.Core.Planning;
using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;
using DeckFlow.Tests.Helpers;

namespace DeckFlow.Tests.Planning;

/// <summary>
/// Tests for bound-location filtering: cards sourced from a deck's own binder
/// should not appear as pull instructions, and carry-over cards should show
/// their actual physical location.
/// </summary>
public sealed class MovementPlanner_BoundLocation_Tests
{
	private readonly IMovementPlanner _planner = new MovementPlanner();

	[Fact]
	public void PreGame_SkipsPulls_FromDecksOwnBinder()
	{
		// Deck has 2 cards: one in its own binder, one in an external binder.
		// Only the external card should appear as a pull instruction.
		var deckId = DeckId.New();
		var deckLocId = LocationId.New();
		var binderId = LocationId.New();

		var ownCard = TestData.Card("Sol Ring");
		var externalCard = TestData.Card("Explore");
		var ownPrint = TestData.Printing("scryfall-sol");
		var externalPrint = TestData.Printing("scryfall-explore");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Ur-Dragon", deckLocId,
				[
					new DeckRequirement(ownCard, 1, null),
					new DeckRequirement(externalCard, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(deckLocId, "Black Side Load", SourceLocationKind.DeckBound, deckId),
				new LocationDefinition(binderId, "Green Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(deckLocId, ownCard, ownPrint, 1),
				new InventoryStack(binderId, externalCard, externalPrint, 1)
			]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);

		// Only the external card should appear
		var group = Assert.Single(deckSet.SourceGroups);
		Assert.Equal("Green Binder", group.LocationName);
		var move = Assert.Single(group.MoveLines);
		Assert.Equal("Explore", move.Card.Name);

		// Sol Ring should NOT appear (it's already in the deck's own binder)
		Assert.DoesNotContain(deckSet.SourceGroups, g =>
			g.MoveLines.Any(m => m.Card.Name == "Sol Ring"));
	}

	[Fact]
	public void PreGame_SelfSourcedCard_StillTrackedAsBorrowed_ReturnsInFinal()
	{
		// Two decks, first deck has a self-sourced card.
		// That card should NOT appear in PreGame pulls but SHOULD be trackable.
		// When the second deck doesn't need it, it should NOT appear in final return
		// (it's already in its home box since the last deck IS that deck).
		var urDragonId = DeckId.New();
		var pantlazaId = DeckId.New();
		var urDragonLocId = LocationId.New();
		var pantlazaLocId = LocationId.New();
		var binderId = LocationId.New();

		var solRing = TestData.Card("Sol Ring");
		var explore = TestData.Card("Explore");
		var solPrint = TestData.Printing("scryfall-sol");
		var explorePrint = TestData.Printing("scryfall-explore");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder:
			[
				new DeckSelection(urDragonId, 1),
				new DeckSelection(pantlazaId, 2)
			],
			AllDecks:
			[
				new DeckDefinition(urDragonId, "Ur-Dragon", urDragonLocId,
				[
					new DeckRequirement(solRing, 1, null)
				]),
				new DeckDefinition(pantlazaId, "Pantlaza", pantlazaLocId,
				[
					new DeckRequirement(explore, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(urDragonLocId, "Black Side Load", SourceLocationKind.DeckBound, urDragonId),
				new LocationDefinition(pantlazaLocId, "Red Deck Box", SourceLocationKind.DeckBound, pantlazaId),
				new LocationDefinition(binderId, "Green Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(urDragonLocId, solRing, solPrint, 1),
				new InventoryStack(binderId, explore, explorePrint, 1)
			]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		// PreGame(Ur-Dragon): Sol Ring skipped (self-sourced)
		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var preSet = Assert.Single(pre.DeckInstructionSets);
		Assert.Empty(preSet.SourceGroups);

		// Transition: Sol Ring not needed by Pantlaza, so it would be returned.
		// But it's self-sourced from Ur-Dragon's own box → return line skipped.
		var transition = Assert.IsType<TransitionStep>(plan.Steps[1]);
		var transSet = Assert.Single(transition.DeckInstructionSets);

		// Only Explore pull from Green Binder should be present
		Assert.Single(transSet.SourceGroups);
		var pullGroup = transSet.SourceGroups.Single(g => g.LocationName == "Green Binder");
		Assert.Single(pullGroup.MoveLines);
		Assert.Equal("Explore", pullGroup.MoveLines[0].Card.Name);

		// Sol Ring should NOT appear in transition (self-sourced, never moved, stays in its box)
		Assert.DoesNotContain(transSet.SourceGroups.SelectMany(g => g.MoveLines),
			m => m.Card.Name == "Sol Ring");
	}

	[Fact]
	public void Transition_CarryOver_ShowsPhysicalLocation_PantlazaFirst()
	{
		// Pantlaza first, Ur-Dragon second.
		// Explore lives in Ur-Dragon's binder. Both decks need Explore.
		// PreGame(Pantlaza): Pull Explore from Black Side Load (shown — not Pantlaza's box)
		// Transition: Explore carry-over, shown as "Pull from Red Deck Box" (Pantlaza's physical location)
		// FinalReturn: Explore already home in Ur-Dragon's box — not shown
		var urDragonId = DeckId.New();
		var pantlazaId = DeckId.New();
		var urDragonLocId = LocationId.New();
		var pantlazaLocId = LocationId.New();

		var explore = TestData.Card("Explore");
		var explorePrint = TestData.Printing("scryfall-explore");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder:
			[
				new DeckSelection(pantlazaId, 1),
				new DeckSelection(urDragonId, 2)
			],
			AllDecks:
			[
				new DeckDefinition(pantlazaId, "Pantlaza", pantlazaLocId,
				[
					new DeckRequirement(explore, 1, null)
				]),
				new DeckDefinition(urDragonId, "Ur-Dragon", urDragonLocId,
				[
					new DeckRequirement(explore, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(urDragonLocId, "Black Side Load", SourceLocationKind.DeckBound, urDragonId),
				new LocationDefinition(pantlazaLocId, "Red Deck Box", SourceLocationKind.DeckBound, pantlazaId)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(urDragonLocId, explore, explorePrint, 1)
			]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);
		Assert.Equal(3, plan.Steps.Count);

		// PreGame(Pantlaza): Pull Explore from Black Side Load
		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var preSet = Assert.Single(pre.DeckInstructionSets);
		var preGroup = Assert.Single(preSet.SourceGroups);
		Assert.Equal("Black Side Load", preGroup.LocationName);
		var preMove = Assert.Single(preGroup.MoveLines);
		Assert.Equal("Explore", preMove.Card.Name);
		Assert.Equal(MoveAction.Pull, preMove.Action);

		// Transition(Pantlaza→Ur-Dragon): Explore carry-over shown from Red Deck Box
		var transition = Assert.IsType<TransitionStep>(plan.Steps[1]);
		var transSet = Assert.Single(transition.DeckInstructionSets);
		var transGroup = Assert.Single(transSet.SourceGroups);
		Assert.Equal("Red Deck Box", transGroup.LocationName);
		var transMove = Assert.Single(transGroup.MoveLines);
		Assert.Equal("Explore", transMove.Card.Name);
		Assert.Equal(MoveAction.Pull, transMove.Action);

		// FinalReturn: Explore is now in Ur-Dragon's box (its home) — not shown
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[2]);
		var finSet = Assert.Single(fin.DeckInstructionSets);
		Assert.Empty(finSet.SourceGroups);
	}

	[Fact]
	public void Transition_CarryOver_ShowsPhysicalLocation_UrDragonFirst()
	{
		// Ur-Dragon first, Pantlaza second.
		// Explore lives in Ur-Dragon's binder. Both decks need Explore.
		// PreGame(Ur-Dragon): Explore skipped (self-sourced from own box)
		// Transition: Explore carry-over, shown as "Pull from Black Side Load" (Ur-Dragon's physical location)
		// FinalReturn: Explore needs to return from Pantlaza to Black Side Load
		var urDragonId = DeckId.New();
		var pantlazaId = DeckId.New();
		var urDragonLocId = LocationId.New();
		var pantlazaLocId = LocationId.New();

		var explore = TestData.Card("Explore");
		var explorePrint = TestData.Printing("scryfall-explore");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder:
			[
				new DeckSelection(urDragonId, 1),
				new DeckSelection(pantlazaId, 2)
			],
			AllDecks:
			[
				new DeckDefinition(urDragonId, "Ur-Dragon", urDragonLocId,
				[
					new DeckRequirement(explore, 1, null)
				]),
				new DeckDefinition(pantlazaId, "Pantlaza", pantlazaLocId,
				[
					new DeckRequirement(explore, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(urDragonLocId, "Black Side Load", SourceLocationKind.DeckBound, urDragonId),
				new LocationDefinition(pantlazaLocId, "Red Deck Box", SourceLocationKind.DeckBound, pantlazaId)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(urDragonLocId, explore, explorePrint, 1)
			]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);
		Assert.Equal(3, plan.Steps.Count);

		// PreGame(Ur-Dragon): Explore skipped (self-sourced from own binder)
		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var preSet = Assert.Single(pre.DeckInstructionSets);
		Assert.Empty(preSet.SourceGroups);

		// Transition(Ur-Dragon→Pantlaza): Explore carry-over shown from Black Side Load
		var transition = Assert.IsType<TransitionStep>(plan.Steps[1]);
		var transSet = Assert.Single(transition.DeckInstructionSets);
		var transGroup = Assert.Single(transSet.SourceGroups);
		Assert.Equal("Black Side Load", transGroup.LocationName);
		var transMove = Assert.Single(transGroup.MoveLines);
		Assert.Equal("Explore", transMove.Card.Name);
		Assert.Equal(MoveAction.Pull, transMove.Action);

		// FinalReturn: Explore in Pantlaza's box, needs to return to Black Side Load
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[2]);
		var finSet = Assert.Single(fin.DeckInstructionSets);
		var finGroup = Assert.Single(finSet.SourceGroups);
		var finMove = Assert.Single(finGroup.MoveLines);
		Assert.Equal("Explore", finMove.Card.Name);
		Assert.Equal(MoveAction.Return, finMove.Action);
		Assert.Equal("Black Side Load", finMove.DestinationDeckName);
	}

	[Fact]
	public void Transition_NonCarryOverReturn_SkipsSelfSourced()
	{
		// Ur-Dragon first, Pantlaza second.
		// Sol Ring is in Ur-Dragon's own box. Only Ur-Dragon needs it.
		// PreGame: Sol Ring skipped (self-sourced)
		// Transition: Sol Ring return skipped (self-sourced, card never left its home)
		var urDragonId = DeckId.New();
		var pantlazaId = DeckId.New();
		var urDragonLocId = LocationId.New();
		var pantlazaLocId = LocationId.New();
		var binderId = LocationId.New();

		var solRing = TestData.Card("Sol Ring");
		var explore = TestData.Card("Explore");
		var solPrint = TestData.Printing("scryfall-sol");
		var explorePrint = TestData.Printing("scryfall-explore");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder:
			[
				new DeckSelection(urDragonId, 1),
				new DeckSelection(pantlazaId, 2)
			],
			AllDecks:
			[
				new DeckDefinition(urDragonId, "Ur-Dragon", urDragonLocId,
				[
					new DeckRequirement(solRing, 1, null)
				]),
				new DeckDefinition(pantlazaId, "Pantlaza", pantlazaLocId,
				[
					new DeckRequirement(explore, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(urDragonLocId, "Black Side Load", SourceLocationKind.DeckBound, urDragonId),
				new LocationDefinition(pantlazaLocId, "Red Deck Box", SourceLocationKind.DeckBound, pantlazaId),
				new LocationDefinition(binderId, "Green Binder", SourceLocationKind.UnboundCollection, null)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(urDragonLocId, solRing, solPrint, 1),
				new InventoryStack(binderId, explore, explorePrint, 1)
			]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);

		// Transition: Only Explore pull from Green Binder. Sol Ring should NOT appear.
		var transition = Assert.IsType<TransitionStep>(plan.Steps[1]);
		var transSet = Assert.Single(transition.DeckInstructionSets);

		Assert.DoesNotContain(transSet.SourceGroups.SelectMany(g => g.MoveLines),
			m => m.Card.Name == "Sol Ring");

		// Green Binder pull for Explore
		var pullGroup = Assert.Single(transSet.SourceGroups);
		Assert.Equal("Green Binder", pullGroup.LocationName);
	}

	[Fact]
	public void FinalReturn_SkipsCardsAlreadyInLastDecksOwnBox()
	{
		// Single deck scenario: card from its own box.
		// PreGame: skipped. FinalReturn: skipped (already home).
		var deckId = DeckId.New();
		var deckLocId = LocationId.New();

		var solRing = TestData.Card("Sol Ring");
		var solPrint = TestData.Printing("scryfall-sol");

		var request = new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: [new DeckSelection(deckId, 1)],
			AllDecks:
			[
				new DeckDefinition(deckId, "Ur-Dragon", deckLocId,
				[
					new DeckRequirement(solRing, 1, null)
				])
			],
			Locations:
			[
				new LocationDefinition(deckLocId, "Black Side Load", SourceLocationKind.DeckBound, deckId)
			],
			Inventory: new InventorySnapshot(
			[
				new InventoryStack(deckLocId, solRing, solPrint, 1)
			]),
			Options: new PlanOptions());

		var plan = _planner.BuildPlan(request);
		Assert.Equal(2, plan.Steps.Count);

		// PreGame: No pulls (self-sourced)
		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var preSet = Assert.Single(pre.DeckInstructionSets);
		Assert.Empty(preSet.SourceGroups);

		// FinalReturn: No returns (card is already at home in own box)
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[1]);
		var finSet = Assert.Single(fin.DeckInstructionSets);
		Assert.Empty(finSet.SourceGroups);
	}
}