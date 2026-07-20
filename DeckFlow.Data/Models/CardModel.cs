namespace DeckFlow.Data.Models;

using SQLite;

[Table("Cards")]
public class CardModel
{
	[PrimaryKey]
	public Guid Id { get; set; }

	[MaxLength(350)]
	[Indexed]
	public string Name { get; set; } = string.Empty;

	[MaxLength(350)]
	public string? BackFaceName { get; set; }
}