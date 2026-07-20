namespace DeckFlow.Core.Parsing;

public static class ManaBoxCsvParser
{
	private static readonly HashSet<string> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
	{
		"Name", "Set code", "Collector number", "Foil", "Quantity", "Scryfall ID", "Binder Name"
	};

	public static IReadOnlyList<ParsedCollectionEntry> Parse(string csvText)
	{
		var lines = csvText.Split('\n', StringSplitOptions.TrimEntries);
		if (lines.Length < 2)
			throw new FormatException("CSV must have header row and at least one data row");

		var header = ParseCsvLine(lines[0]);
		var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < header.Count; i++)
			columnMap[header[i]] = i;

		foreach (var required in RequiredColumns)
		{
			if (!columnMap.ContainsKey(required))
				throw new FormatException($"Missing required column: {required}");
		}

		var results = new List<ParsedCollectionEntry>();

		for (int lineNum = 1; lineNum < lines.Length; lineNum++)
		{
			if (string.IsNullOrWhiteSpace(lines[lineNum]))
				continue;

			var fields = ParseCsvLine(lines[lineNum]);
			if (fields.Count < columnMap.Count)
				continue;

			var scryfallId = GetField(fields, columnMap, "Scryfall ID");
			if (!Guid.TryParse(scryfallId, out _))
				continue;

			var (frontFace, backFace) = CardNameHelper.SplitCardName(GetField(fields, columnMap, "Name"));

			results.Add(new ParsedCollectionEntry(
				LocationName: GetField(fields, columnMap, "Binder Name"),
				CardName: frontFace,
				BackFaceName: backFace,
				SetCode: GetField(fields, columnMap, "Set code"),
				SetName: GetField(fields, columnMap, "Set name"),
				CollectorNumber: GetField(fields, columnMap, "Collector number").TrimEnd('★'),
				IsFoil: GetField(fields, columnMap, "Foil").Equals("foil", StringComparison.OrdinalIgnoreCase),
				Quantity: int.Parse(GetField(fields, columnMap, "Quantity")),
				ScryfallId: scryfallId));
		}

		return results;
	}

	private static string GetField(List<string> fields, Dictionary<string, int> map, string column)
		=> fields[map[column]];

	private static List<string> ParseCsvLine(string line)
	{
		var fields = new List<string>();
		var current = new System.Text.StringBuilder();
		bool inQuotes = false;

		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			if (c == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					current.Append('"');
					i++;
				}
				else
				{
					inQuotes = !inQuotes;
				}
			}
			else if (c == ',' && !inQuotes)
			{
				fields.Add(current.ToString());
				current.Clear();
			}
			else
			{
				current.Append(c);
			}
		}
		fields.Add(current.ToString());
		return fields;
	}
}