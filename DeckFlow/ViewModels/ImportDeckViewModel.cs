namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Core.Parsing;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Services;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion
#pragma warning disable MVVMTK0034 // Direct field reference

public partial class ImportDeckViewModel(
	IDeckImportService importService,
	ILocationRepository locationRepo,
	IDeckRepository deckRepo) : ObservableObject
{
	[ObservableProperty]
	private string _fileName = string.Empty;

	[ObservableProperty]
	private string _deckName = string.Empty;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isError;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasPreview))]
	private int _previewCardCount;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasPreview))]
	private int _previewUniqueCount;

	[ObservableProperty]
	private bool _isImporting;

	[ObservableProperty]
	private LocationItem? _selectedLocation;

	public bool HasPreview => PreviewCardCount > 0;

	public ObservableCollection<LocationItem> Locations { get; } = [];

	private string? _decklistText;

	public async Task LoadLocationsAsync()
	{
		var locations = await locationRepo.GetAllAsync();
		var decks = await deckRepo.GetAllAsync();
		var boundLocationIds = decks
			.Where(d => d.BoundLocationId.HasValue)
			.Select(d => d.BoundLocationId!.Value)
			.ToHashSet();

		Locations.Clear();
		Locations.Add(new LocationItem(null, "Unbound (no location)"));

		foreach (var loc in locations.OrderBy(l => l.Name))
		{
			if (!loc.AvailableForDeckAssignment)
				continue;

			if (!loc.AllowMultipleDecks && boundLocationIds.Contains(loc.Id))
				continue;

			Locations.Add(new LocationItem(loc.Id, loc.Name));
		}

		SelectedLocation = Locations[0];
	}

	[RelayCommand]
	private async Task PickFileAsync()
	{
		try
		{
			var result = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Select deck list file",
				FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{ DevicePlatform.Android, new[] { "text/plain", "application/octet-stream" } },
					{ DevicePlatform.WinUI, new[] { ".txt", ".dec", ".dck" } }
				})
			});

			if (result is null)
				return;

			FileName = result.FileName;
			StatusMessage = string.Empty;
			IsError = false;

			if (string.IsNullOrWhiteSpace(DeckName))
				DeckName = Path.GetFileNameWithoutExtension(result.FileName);

			using var stream = await result.OpenReadAsync();
			using var reader = new StreamReader(stream);
			_decklistText = await reader.ReadToEndAsync();

			if (_decklistText.StartsWith("%PDF", StringComparison.Ordinal))
				throw new FormatException(
					"The file was opened as a PDF preview. Try saving the file to your device first, then import from local storage.");

			var entries = DecklistParser.Parse(_decklistText);
			PreviewCardCount = entries.Sum(e => e.Quantity);
			PreviewUniqueCount = entries.Count;
		}
		catch (FormatException ex)
		{
			_decklistText = null;
			PreviewCardCount = 0;
			PreviewUniqueCount = 0;
			StatusMessage = $"Parse error: {ex.Message}";
			IsError = true;
		}
		catch (Exception ex)
		{
			_decklistText = null;
			PreviewCardCount = 0;
			PreviewUniqueCount = 0;
			StatusMessage = $"Error reading file: {ex.Message}";
			IsError = true;
		}
	}

	[RelayCommand]
	private async Task CreateDeckAsync()
	{
		if (_decklistText is null || string.IsNullOrWhiteSpace(DeckName))
			return;

		try
		{
			IsImporting = true;
			IsError = false;

			await importService.ImportAsync(_decklistText, DeckName.Trim(), SelectedLocation?.Id);
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

public record LocationItem(Guid? Id, string Name)
{
	public override string ToString() => Name;
}