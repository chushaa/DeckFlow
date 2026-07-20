namespace DeckFlow.Core.Planning;

using DeckFlow.Core.Planning.Contracts;

/// <summary>
/// Generates movement plans for multi-deck play sessions.
/// </summary>
public interface IMovementPlanner
{
	/// <summary>
	/// Builds a complete movement plan from the given request.
	/// </summary>
	/// <remarks>
	/// This operation is synchronous and CPU-bound.
	/// All data is already loaded in the request; no I/O occurs.
	/// </remarks>
	MovementPlan BuildPlan(PlanRequest request);
}