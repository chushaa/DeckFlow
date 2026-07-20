namespace DeckFlow.Data.Models;

using SQLite;

[Table("OwnedCopies")]
public class OwnedCopyModel
{
	[PrimaryKey]
	public Guid Id { get; set; }

	[Indexed]
	public Guid CardId { get; set; }

	[MaxLength(36)]
	[Indexed]
	public string ScryfallId { get; set; } = string.Empty;

	[Indexed]
	public Guid LocationId { get; set; }

	public int Quantity { get; set; }
}