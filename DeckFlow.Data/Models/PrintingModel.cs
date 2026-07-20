namespace DeckFlow.Data.Models;

using SQLite;

[Table("Printings")]
public class PrintingModel
{
	[PrimaryKey]
	[MaxLength(36)]
	public string ScryfallId { get; set; } = string.Empty;

	[Indexed]
	public Guid CardId { get; set; }

	[MaxLength(10)]
	public string SetCode { get; set; } = string.Empty;

	[MaxLength(100)]
	public string SetName { get; set; } = string.Empty;

	[MaxLength(20)]
	public string CollectorNumber { get; set; } = string.Empty;

	public bool IsFoil { get; set; }
}