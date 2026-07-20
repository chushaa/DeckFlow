namespace DeckFlow.Core.Planning.Contracts;

public enum PlanStepType { PreGame, Transition, FinalReturn }

public enum SourceLocationKind { Unknown, UnboundCollection, DeckBound }

public enum MoveAction { Pull, Move, Return }

public enum AllocationReason
{
	ExactPrintingMatch,
	FromUnselectedDeck,
	FromUnboundCollection,
	FromSelectedDeck
}

public enum PlannerNoteLevel { Info, Warning, Error }