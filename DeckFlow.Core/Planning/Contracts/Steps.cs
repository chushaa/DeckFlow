namespace DeckFlow.Core.Planning.Contracts;

using DeckFlow.Core.ValueObjects;

public abstract record PlanStep(
	int StepIndex,
	PlanStepType StepType,
	string Title,
	string Subtitle,
	IReadOnlyList<DeckInstructionSet> DeckInstructionSets,
	IReadOnlyList<PlannerNote> Notes);

public sealed record PreGameStep(
	int StepIndex,
	string Title,
	string Subtitle,
	DeckId TargetDeckId,
	string TargetDeckName,
	IReadOnlyList<DeckInstructionSet> DeckInstructionSets,
	IReadOnlyList<PlannerNote> Notes)
	: PlanStep(StepIndex, PlanStepType.PreGame, Title, Subtitle, DeckInstructionSets, Notes);

public sealed record TransitionStep(
	int StepIndex,
	string Title,
	string Subtitle,
	DeckId FromDeckId,
	string FromDeckName,
	DeckId ToDeckId,
	string ToDeckName,
	IReadOnlyList<DeckInstructionSet> DeckInstructionSets,
	IReadOnlyList<PlannerNote> Notes)
	: PlanStep(StepIndex, PlanStepType.Transition, Title, Subtitle, DeckInstructionSets, Notes);

public sealed record FinalReturnStep(
	int StepIndex,
	string Title,
	string Subtitle,
	IReadOnlyList<DeckInstructionSet> DeckInstructionSets,
	IReadOnlyList<PlannerNote> Notes)
	: PlanStep(StepIndex, PlanStepType.FinalReturn, Title, Subtitle, DeckInstructionSets, Notes);