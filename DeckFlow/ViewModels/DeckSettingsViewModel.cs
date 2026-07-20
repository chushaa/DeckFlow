namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Core.Parsing;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Services;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion
#pragma warning disable MVVMTK0034 // Direct field reference

public partial class DeckSettingsViewModel(
	IDeckRepository deckRepo,
	IDeckRequirementRepository reqRepo,
	IDeckImportService importService,
	ILocationRepository locationRepo) : ObservableObject, IQueryAttributable
{
	private Guid _deckId;

	[ObservableProperty]
	private string _deckName = string.Empty;

	[ObservableProperty]
	private int _cardCount;

	// Edit Name
	[ObservableProperty]
	private string _originalName = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSaveName))]
	private string _editedName = string.Empty;

	public bool CanSaveName =>
		!string.IsNullOrWhiteSpace(EditedName) &&
		EditedName.Trim() != OriginalName;

	// Change Location
	[ObservableProperty]
	private LocationItem? _selectedLocation;

	public ObservableCollection<LocationItem> Locations { get; } = [];

	// Replace Cards
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasReplacePreview))]
	private string _replacePreviewText = string.Empty;

	[ObservableProperty]
	private bool _isReplacing;

	public bool HasReplacePreview => !string.IsNullOrEmpty(ReplacePreviewText);

	private string? _replaceText;

	// Status
	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isError;

	[ObservableProperty]
	private bool _isLoaded;

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("DeckId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id))
			_deckId = id;
	}

	public async Task LoadAsync()
	{
		var deck = await deckRepo.GetByIdAsync(_deckId);
		if (deck is null)
		{
			StatusMessage = "Deck not found.";
			IsError = true;
			return;
		}

		DeckName = deck.Name;
		OriginalName = deck.Name;
		EditedName = deck.Name;

		var reqs = await reqRepo.GetByDeckIdAsync(_deckId);
		CardCount = reqs.Sum(r => r.Quantity);

		var locations = await locationRepo.GetAllAsync();
		var allDecks = await deckRepo.GetAllAsync();
		var boundLocationIds = allDecks
			.Where(d => d.BoundLocationId.HasValue && d.Id != _deckId)
			.Select(d => d.BoundLocationId!.Value)
			.ToHashSet();

		Locations.Clear();
		Locations.Add(new LocationItem(null, "Unbound (no location)"));

		foreach (var loc in locations.OrderBy(l => l.Name))
		{
			// Always show the current deck's own binding
			var isOwnBinding = deck.BoundLocationId.HasValue && deck.BoundLocationId.Value == loc.Id;

			if (!isOwnBinding)
			{
				if (!loc.AvailableForDeckAssignment)
					continue;

				if (!loc.AllowMultipleDecks && boundLocationIds.Contains(loc.Id))
					continue;
			}

			Locations.Add(new LocationItem(loc.Id, loc.Name));
		}

		SelectedLocation = deck.BoundLocationId.HasValue
			? Locations.FirstOrDefault(l => l.Id == deck.BoundLocationId) ?? Locations[0]
			: Locations[0];

		IsLoaded = true;
	}

	[RelayCommand]
	private async Task SaveNameAsync()
	{
		if (!CanSaveName)
			return;

		try
		{
			var deck = await deckRepo.GetByIdAsync(_deckId);
			if (deck is null)
				return;

			deck.Name = EditedName.Trim();
			await deckRepo.UpdateAsync(deck);
			OriginalName = deck.Name;
			DeckName = deck.Name;
			OnPropertyChanged(nameof(CanSaveName));
			StatusMessage = "Name saved.";
			IsError = false;
		}
		catch (Exception ex)
		{
			StatusMessage = $"Failed to save name: {ex.Message}";
			IsError = true;
		}
	}

	[RelayCommand]
	private async Task SaveLocationAsync()
	{
		try
		{
			var deck = await deckRepo.GetByIdAsync(_deckId);
			if (deck is null)
				return;

			deck.BoundLocationId = SelectedLocation?.Id;
			await deckRepo.UpdateAsync(deck);
			StatusMessage = "Location saved.";
			IsError = false;
		}
		catch (Exception ex)
		{
			StatusMessage = $"Failed to save location: {ex.Message}";
			IsError = true;
		}
	}

	[RelayCommand]
	private async Task PickReplaceFileAsync()
	{
		try
		{
			var result = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Select new deck list file",
				FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{ DevicePlatform.Android, new[] { "text/plain", "application/octet-stream" } },
					{ DevicePlatform.WinUI, new[] { ".txt", ".dec", ".dck" } }
				})
			});

			if (result is null)
				return;

			using var stream = await result.OpenReadAsync();
			using var reader = new StreamReader(stream);
			_replaceText = await reader.ReadToEndAsync();

			if (_replaceText.StartsWith("%PDF", StringComparison.Ordinal))
				throw new FormatException(
					"The file was opened as a PDF preview. Try saving the file to your device first, then import from local storage.");

			var entries = DecklistParser.Parse(_replaceText);
			var totalCards = entries.Sum(e => e.Quantity);
			ReplacePreviewText = $"{result.FileName}: {totalCards} cards ({entries.Count} unique)";
			StatusMessage = string.Empty;
			IsError = false;
		}
		catch (FormatException ex)
		{
			_replaceText = null;
			ReplacePreviewText = string.Empty;
			StatusMessage = $"Parse error: {ex.Message}";
			IsError = true;
		}
		catch (Exception ex)
		{
			_replaceText = null;
			ReplacePreviewText = string.Empty;
			StatusMessage = $"Error reading file: {ex.Message}";
			IsError = true;
		}
	}

	[RelayCommand]
	private async Task ConfirmReplaceAsync()
	{
		if (_replaceText is null)
			return;

		try
		{
			IsReplacing = true;
			IsError = false;

			await importService.ReplaceRequirementsAsync(_deckId, _replaceText);

			var reqs = await reqRepo.GetByDeckIdAsync(_deckId);
			CardCount = reqs.Sum(r => r.Quantity);

			_replaceText = null;
			ReplacePreviewText = string.Empty;
			StatusMessage = $"Cards replaced. Now {CardCount} cards.";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Replace failed: {ex.Message}";
			IsError = true;
		}
		finally
		{
			IsReplacing = false;
		}
	}

	[RelayCommand]
	private async Task DeleteDeckAsync()
	{
		var confirm = await Shell.Current.DisplayAlertAsync(
			"Delete Deck",
			$"Are you sure you want to delete \"{DeckName}\"? This cannot be undone.",
			"Delete",
			"Cancel");

		if (!confirm)
			return;

		try
		{
			await reqRepo.DeleteByDeckIdAsync(_deckId);
			await deckRepo.DeleteAsync(_deckId);
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			StatusMessage = $"Delete failed: {ex.Message}";
			IsError = true;
		}
	}

	[RelayCommand]
	private async Task GoBackAsync()
	{
		await Shell.Current.GoToAsync("..");
	}
}