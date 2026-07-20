namespace DeckFlow.Data.Models;

using SQLite;

[Table("DeckRequirements")]
public class DeckRequirementModel
{
	[PrimaryKey]
	public Guid Id { get; set; }

	[Indexed]
	public Guid DeckId { get; set; }

	[Indexed]
	public Guid CardId { get; set; }

	public int Quantity { get; set; }

	[MaxLength(36)]
	public string? RequestedScryfallId { get; set; }

	[MaxLength(10)]
	public string? RequestedSetCode { get; set; }

	[MaxLength(20)]
	public string? RequestedCollectorNumber { get; set; }
}