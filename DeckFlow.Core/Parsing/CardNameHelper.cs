namespace DeckFlow.Core.Parsing;

public static class CardNameHelper
{
	public static string JoinCardName(string frontFace, string? backFace)
		=> backFace is not null ? $"{frontFace} // {backFace}" : frontFace;

	public static (string FrontFace, string? BackFace) SplitCardName(string name)
	{
		// ManaBox/Scryfall format: " // "
		int doubleSlash = name.IndexOf(" // ", StringComparison.Ordinal);
		if (doubleSlash >= 0)
			return (name[..doubleSlash].Trim(), name[(doubleSlash + 4)..].Trim());

		// Deck list format: " / "
		int singleSlash = name.IndexOf(" / ", StringComparison.Ordinal);
		if (singleSlash >= 0)
			return (name[..singleSlash].Trim(), name[(singleSlash + 3)..].Trim());

		return (name, null);
	}
}