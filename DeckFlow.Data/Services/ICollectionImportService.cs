namespace DeckFlow.Data.Services;

using DeckFlow.Core.Parsing;

public interface ICollectionImportService
{
	Task<int> ImportAsync(string csvText, IProgress<(int Current, int Total)>? progress = null);
	Task<int> ImportAsync(IReadOnlyList<ParsedCollectionEntry> entries, IProgress<(int Current, int Total)>? progress = null);
}