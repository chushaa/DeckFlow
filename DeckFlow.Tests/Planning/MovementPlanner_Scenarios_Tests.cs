using DeckFlow.Core.Planning;
using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Tests.Helpers;

namespace DeckFlow.Tests.Planning;

public sealed class MovementPlanner_Scenarios_Tests
{
	private readonly IMovementPlanner _planner = new MovementPlanner();

	[Fact]
	public void Scenario1_SingleDeck_PullsFromBinder_AndReturnsToBinder()
	{
		var (req, ids) = TestData.BuildScenario1();

		var plan = _planner.BuildPlan(req);

		// Steps: PreGame for Deck A + FinalReturn
		Assert.Equal(2, plan.Steps.Count);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		Assert.Equal(ids.DeckA, pre.TargetDeckId);

		var deckSet = Assert.Single(pre.DeckInstructionSets);
		Assert.Equal("Deck A", deckSet.DeckName);

		// Grouped by source: Green
		var greenGroup = Assert.Single(deckSet.SourceGroups);
		Assert.Equal("Green", greenGroup.LocationName);

		var move = Assert.Single(greenGroup.MoveLines);
		Assert.Equal(MoveAction.Pull, move.Action);
		Assert.Equal("Explore", move.Card.Name);
		Assert.Equal(1, move.Quantity);
		Assert.Equal("Deck A", move.DestinationDeckName);

		// Final return: Explore goes back to Green
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[1]);
		var finDeckSet = Assert.Single(fin.DeckInstructionSets);

		AssertContainsReturn(finDeckSet, "Explore", 1, "Green");
	}

	[Fact]
	public void Scenario2_SingleDeck_PullsFromOtherDeck_AndReturnsToThatDeck()
	{
		var (req, ids) = TestData.BuildScenario2();

		var plan = _planner.BuildPlan(req);

		Assert.Equal(2, plan.Steps.Count);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		Assert.Equal(ids.DeckB, pre.TargetDeckId);

		var deckSet = Assert.Single(pre.DeckInstructionSets);

		// Source group should be Deck C
		Assert.Contains(deckSet.SourceGroups, g => g.LocationName == "Deck C");

		var deckCGroup = deckSet.SourceGroups.Single(g => g.LocationName == "Deck C");
		var move = Assert.Single(deckCGroup.MoveLines);

		Assert.Equal(MoveAction.Pull, move.Action);
		Assert.Equal("Shock", move.Card.Name);
		Assert.Equal(1, move.Quantity);
		Assert.Equal("Deck B", move.DestinationDeckName);

		// Final return: Shock goes back to Deck C
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[1]);
		AssertStepContainsReturn(fin, "Shock", 1, "Deck C");
	}

	[Fact]
	public void Scenario3_AThenB_FuryCarriedOver_ShownFromDeckA_FinalDoesNotMentionFury()
	{
		var (req, ids) = TestData.BuildScenario3_AThenB();

		var plan = _planner.BuildPlan(req);

		// Steps: PreGame(A), Transition(A→B), FinalReturn
		Assert.Equal(3, plan.Steps.Count);

		var step0 = Assert.IsType<PreGameStep>(plan.Steps[0]);
		Assert.Equal(ids.DeckA, step0.TargetDeckId);

		// PreGame(A) includes: Explore from Green + Fury from Deck B
		AssertPreGameContainsPull(step0, "Explore", 1, "Green");
		AssertPreGameContainsPull(step0, "Fury", 1, "Deck B");

		var step1 = Assert.IsType<TransitionStep>(plan.Steps[1]);
		Assert.Equal(ids.DeckA, step1.FromDeckId);
		Assert.Equal(ids.DeckB, step1.ToDeckId);

		// Transition: Explore returned to Green (not needed by B), Shock pulled from Deck C
		// Fury carried over — shown as pull from Deck A (its physical location)
		AssertTransitionContainsReturn(step1, "Explore", 1, "Green");
		AssertTransitionContainsPull(step1, "Shock", 1, "Deck C");
		AssertTransitionContainsPull(step1, "Fury", 1, "Deck A");

		// FinalReturn: only Shock→Deck C. Fury is at Deck B (its home). Explore already returned.
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[2]);
		AssertStepContainsReturn(fin, "Shock", 1, "Deck C");
		AssertStepDoesNotContainCard(fin, "Fury");
		AssertStepDoesNotContainCard(fin, "Explore");
	}

	[Fact]
	public void Scenario4_BThenA_FurySkippedInPreGame_ShownInTransition_FinalReturnsFuryToDeckB()
	{
		var (req, ids) = TestData.BuildScenario4_BThenA();

		var plan = _planner.BuildPlan(req);

		// Steps: PreGame(B), Transition(B→A), FinalReturn
		Assert.Equal(3, plan.Steps.Count);

		var step0 = Assert.IsType<PreGameStep>(plan.Steps[0]);
		Assert.Equal(ids.DeckB, step0.TargetDeckId);

		// PreGame(B): Only Shock from Deck C. Fury is skipped (self-sourced from Deck B's own box)
		AssertPreGameContainsPull(step0, "Shock", 1, "Deck C");
		AssertPreGameDoesNotContainCard(step0, "Fury");

		var step1 = Assert.IsType<TransitionStep>(plan.Steps[1]);
		Assert.Equal(ids.DeckB, step1.FromDeckId);
		Assert.Equal(ids.DeckA, step1.ToDeckId);

		// Transition: Return Shock to Deck C, Pull Explore from Green
		// Fury carried over — shown as pull from Deck B (its physical location)
		AssertTransitionContainsReturn(step1, "Shock", 1, "Deck C");
		AssertTransitionContainsPull(step1, "Explore", 1, "Green");
		AssertTransitionContainsPull(step1, "Fury", 1, "Deck B");

		// FinalReturn: Explore→Green, Fury→Deck B (it ended in Deck A, needs to go home)
		// Shock was already returned during transition
		var fin = Assert.IsType<FinalReturnStep>(plan.Steps[2]);
		AssertStepContainsReturn(fin, "Explore", 1, "Green");
		AssertStepContainsReturn(fin, "Fury", 1, "Deck B");
		AssertStepDoesNotContainCard(fin, "Shock");
	}

	[Fact]
	public void Guardrail_ConsumesFullyFromSourceBeforeNextSource()
	{
		var (req, _) = TestData.BuildGuardrail_ConsumeFully();

		var plan = _planner.BuildPlan(req);

		var pre = Assert.IsType<PreGameStep>(plan.Steps[0]);
		var deckSet = Assert.Single(pre.DeckInstructionSets);

		// Locations sorted alphabetically: "Binder A" then "Binder B"
		Assert.Equal(2, deckSet.SourceGroups.Count);
		Assert.Equal("Binder A", deckSet.SourceGroups[0].LocationName);
		Assert.Equal("Binder B", deckSet.SourceGroups[1].LocationName);

		var movesA = deckSet.SourceGroups[0].MoveLines;
		var movesB = deckSet.SourceGroups[1].MoveLines;

		Assert.Single(movesA);
		Assert.Equal("Opt", movesA[0].Card.Name);
		Assert.Equal(2, movesA[0].Quantity);

		Assert.Single(movesB);
		Assert.Equal("Opt", movesB[0].Card.Name);
		Assert.Equal(1, movesB[0].Quantity);
	}

	[Fact]
	public void Plan_Summary_ReflectsCorrectTotals()
	{
		var (req, _) = TestData.BuildScenario3_AThenB();

		var plan = _planner.BuildPlan(req);

		Assert.Equal(3, plan.Summary.TotalSteps);
		Assert.True(plan.Summary.TotalMoveLines > 0);
		Assert.True(plan.Summary.TotalCardsToMove > 0);
		Assert.Equal(0, plan.Summary.TotalMissingCards);
	}

	[Fact]
	public void Plan_HasValidPlanId_And_CreatedUtc()
	{
		var (req, _) = TestData.BuildScenario1();

		var plan = _planner.BuildPlan(req);

		Assert.NotEqual(Guid.Empty, plan.PlanId);
		Assert.True(plan.CreatedUtc > DateTime.MinValue);
	}

	[Fact]
	public void Plan_SelectedDecksInOrder_ReflectsInputOrder()
	{
		var (req, ids) = TestData.BuildScenario3_AThenB();

		var plan = _planner.BuildPlan(req);

		Assert.Equal(2, plan.SelectedDecksInOrder.Count);
		Assert.Equal(ids.DeckA, plan.SelectedDecksInOrder[0].DeckId);
		Assert.Equal(ids.DeckB, plan.SelectedDecksInOrder[1].DeckId);
		Assert.Equal(1, plan.SelectedDecksInOrder[0].OrderIndex);
		Assert.Equal(2, plan.SelectedDecksInOrder[1].OrderIndex);
	}

	#region Assertion Helpers

	private static void AssertPreGameDoesNotContainCard(PreGameStep step, string cardName)
	{
		var deckSet = Assert.Single(step.DeckInstructionSets);
		Assert.False(deckSet.SourceGroups
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Card.Name == cardName),
			$"Expected {cardName} NOT to appear in pre-game");
	}

	private static void AssertPreGameContainsPull(PreGameStep step, string cardName, int qty, string sourceLocation)
	{
		var deckSet = Assert.Single(step.DeckInstructionSets);
		var group = deckSet.SourceGroups.SingleOrDefault(g => g.LocationName == sourceLocation);
		Assert.NotNull(group);
		Assert.Contains(group.MoveLines, m =>
			m.Action == MoveAction.Pull &&
			m.Card.Name == cardName &&
			m.Quantity == qty);
	}

	private static void AssertTransitionContainsReturn(TransitionStep step, string cardName, int qty, string destinationName)
	{
		Assert.True(step.DeckInstructionSets
			.SelectMany(s => s.SourceGroups)
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Action == MoveAction.Return
				&& m.Card.Name == cardName
				&& m.Quantity == qty
				&& m.DestinationDeckName == destinationName),
			$"Expected return of {qty}x {cardName} to {destinationName} in transition");
	}

	private static void AssertTransitionContainsPull(TransitionStep step, string cardName, int qty, string sourceLocation)
	{
		Assert.True(step.DeckInstructionSets
			.SelectMany(s => s.SourceGroups)
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Action == MoveAction.Pull
				&& m.Card.Name == cardName
				&& m.Quantity == qty
				&& m.SourceLocationName == sourceLocation),
			$"Expected pull of {qty}x {cardName} from {sourceLocation} in transition");
	}

	private static void AssertTransitionDoesNotContainCard(TransitionStep step, string cardName)
	{
		Assert.False(step.DeckInstructionSets
			.SelectMany(s => s.SourceGroups)
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Card.Name == cardName),
			$"Expected {cardName} NOT to appear in transition");
	}

	private static void AssertStepContainsReturn(FinalReturnStep step, string cardName, int qty, string destinationName)
	{
		Assert.True(step.DeckInstructionSets
			.SelectMany(s => s.SourceGroups)
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Action == MoveAction.Return
				&& m.Card.Name == cardName
				&& m.Quantity == qty
				&& m.DestinationDeckName == destinationName),
			$"Expected return of {qty}x {cardName} to {destinationName}");
	}

	private static void AssertContainsReturn(DeckInstructionSet deckSet, string cardName, int qty, string destinationName)
	{
		Assert.True(deckSet.SourceGroups
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Action == MoveAction.Return
				&& m.Card.Name == cardName
				&& m.Quantity == qty
				&& m.DestinationDeckName == destinationName),
			$"Expected return of {qty}x {cardName} to {destinationName}");
	}

	private static void AssertStepDoesNotContainCard(FinalReturnStep step, string cardName)
	{
		Assert.False(step.DeckInstructionSets
			.SelectMany(s => s.SourceGroups)
			.SelectMany(g => g.MoveLines)
			.Any(m => m.Card.Name == cardName),
			$"Expected {cardName} NOT to appear in final return");
	}

	#endregion Assertion Helpers
}