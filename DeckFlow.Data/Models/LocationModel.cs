namespace DeckFlow.Data.Models;

using SQLite;

[Table("Locations")]
public class LocationModel
{
	[PrimaryKey]
	public Guid Id { get; set; }

	[MaxLength(100)]
	[Indexed]
	public string Name { get; set; } = string.Empty;

	[MaxLength(9)]
	public string Color { get; set; } = "#2D8A96";

	public bool AvailableForDeckAssignment { get; set; } = true;

	public bool AllowMultipleDecks { get; set; }
}