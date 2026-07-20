namespace DeckFlow.Data.Models;

using SQLite;

[Table("Decks")]
public class DeckModel
{
	[PrimaryKey]
	public Guid Id { get; set; }

	[MaxLength(100)]
	public string Name { get; set; } = string.Empty;

	public Guid? BoundLocationId { get; set; }
}