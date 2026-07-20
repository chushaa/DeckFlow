namespace DeckFlow.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Core.Parsing;
using DeckFlow.Data.Services;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion
#pragma warning disable MVVMTK0034 // Direct field reference

public partial class ImportCollectionViewModel(ICollectionImportService importService) : ObservableObject
{
	[ObservableProperty]
	private string _fileName = string.Empty;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isError;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasPreview))]
	private int _previewCardCount;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasPreview))]
	private int _previewLocationCount;

	[ObservableProperty]
	private bool _isImporting;

	[ObservableProperty]
	private bool _importComplete;

	[ObservableProperty]
	private int _importedEntryCount;

	[ObservableProperty]
	private int _totalEntryCount;

	[ObservableProperty]
	private double _importProgress;

	public bool HasPreview => PreviewCardCount > 0;

	private IReadOnlyList<ParsedCollectionEntry>? _parsedEntries;

	[RelayCommand]
	private async Task PickFileAsync()
	{
		try
		{
			var result = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Select ManaBox CSV export",
				FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{ DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "text/plain", "application/octet-stream" } },
					{ DevicePlatform.WinUI, new[] { ".csv" } }
				})
			});

			if (result is null)
				return;

			FileName = result.FileName;
			StatusMessage = string.Empty;
			IsError = false;
			ImportComplete = false;

            await using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var csvText = await reader.ReadToEndAsync();

			if (csvText.StartsWith("%PDF", StringComparison.Ordinal))
				throw new FormatException(
					"The file was opened as a PDF preview. Try saving the file to your device first, then import from local storage.");

			_parsedEntries = ManaBoxCsvParser.Parse(csvText);
			PreviewCardCount = _parsedEntries.Sum(e => e.Quantity);
			PreviewLocationCount = _parsedEntries.Select(e => e.LocationName).Distinct().Count();
			StatusMessage = $"Found {PreviewCardCount} cards across {PreviewLocationCount} locations";
		}
		catch (FormatException ex)
		{
			_parsedEntries = null;
			PreviewCardCount = 0;
			PreviewLocationCount = 0;
			StatusMessage = $"Parse error: {ex.Message}";
			IsError = true;
		}
		catch (Exception ex)
		{
			_parsedEntries = null;
			PreviewCardCount = 0;
			PreviewLocationCount = 0;
			StatusMessage = $"Error reading file: {ex.Message}";
			IsError = true;
		}
	}

	[RelayCommand]
	private async Task ImportAsync()
	{
		if (_parsedEntries is null)
			return;

		try
		{
			IsImporting = true;
			IsError = false;
			ImportedEntryCount = 0;
			TotalEntryCount = 0;
			ImportProgress = 0;

			var progress = new Progress<(int Current, int Total)>(p =>
			{
				ImportedEntryCount = p.Current;
				TotalEntryCount = p.Total;
				ImportProgress = p.Total > 0 ? (double)p.Current / p.Total : 0;
			});

			var count = await importService.ImportAsync(_parsedEntries, progress);
			StatusMessage = $"Successfully imported {count} cards";
			ImportComplete = true;

			await Task.Delay(500);
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			StatusMessage = $"Import failed: {ex.Message}";
			IsError = true;
		}
		finally
		{
			IsImporting = false;
		}
	}

}