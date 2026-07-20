namespace DeckFlow.Core.Parsing;

using System.Text.RegularExpressions;

public static partial class DecklistParser
{
	// Pattern: "1 Card Name (SET) 123 *F*"
	// Groups: 1=qty, 2=name, 3=set (optional), 4=collector (optional), 5=foil marker (optional)
	[GeneratedRegex(@"^(\d+)\s+(.+?)\s*(?:\((\w{2,5})\)\s+(\S+?))?\s*(\*[FEfe]\*)?$",
		RegexOptions.Compiled)]
	private static partial Regex LinePattern();

	public static IReadOnlyList<ParsedDeckEntry> Parse(string text)
	{
		var results = new List<ParsedDeckEntry>();
		var lines = text.Split('\n', StringSplitOptions.TrimEntries);

		foreach (var line in lines)
		{
			if (string.IsNullOrWhiteSpace(line) ||
				line.StartsWith('#') ||
				line.StartsWith("//"))
				continue;

			var match = LinePattern().Match(line);
			if (!match.Success)
				throw new FormatException($"Invalid deck entry: {line}");

			var quantity = int.Parse(match.Groups[1].Value);
			var rawName = match.Groups[2].Value.Trim();
			var (frontFace, backFace) = CardNameHelper.SplitCardName(rawName);
			var setCode = match.Groups[3].Success ? match.Groups[3].Value : null;
			var collector = match.Groups[4].Success
				? match.Groups[4].Value.TrimEnd('★', '*')
				: null;
			var foilMarker = match.Groups[5].Success
				? match.Groups[5].Value.ToUpperInvariant()
				: null;

			results.Add(new ParsedDeckEntry(
				Quantity: quantity,
				CardName: frontFace,
				BackFaceName: backFace,
				SetCode: setCode,
				CollectorNumber: collector,
				IsFoil: foilMarker == "*F*",
				IsEtched: foilMarker == "*E*"));
		}

		return results;
	}
}