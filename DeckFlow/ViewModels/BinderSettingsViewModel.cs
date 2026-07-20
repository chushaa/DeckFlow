namespace DeckFlow.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Data.Abstractions;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class BinderSettingsViewModel(ILocationRepository locationRepo) : ObservableObject, IQueryAttributable
{
	private Guid _locationId;

	[ObservableProperty]
	private string _binderName = string.Empty;

	[ObservableProperty]
	private string _selectedColor = "#2D8A96";

	[ObservableProperty]
	private bool _availableForDeckAssignment = true;

	[ObservableProperty]
	private bool _allowMultipleDecks;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isLoaded;

	public List<string> ColorPalette { get; } =
	[
		"#2D8A96", "#388296", "#DC2626", "#F59E0B",
		"#10B981", "#3B82F6", "#8B5CF6", "#EC4899",
		"#6B7280", "#1F2937", "#D97706", "#059669"
	];

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("LocationId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id))
			_locationId = id;
	}

	public async Task LoadAsync()
	{
		var location = await locationRepo.GetByIdAsync(_locationId);
		if (location is null)
		{
			StatusMessage = "Binder not found.";
			return;
		}

		BinderName = location.Name;
		SelectedColor = location.Color;
		AvailableForDeckAssignment = location.AvailableForDeckAssignment;
		AllowMultipleDecks = location.AllowMultipleDecks;
		IsLoaded = true;
	}

	[RelayCommand]
	private void SelectColor(string hex)
	{
		SelectedColor = hex;
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		try
		{
			var location = await locationRepo.GetByIdAsync(_locationId);
			if (location is null)
				return;

			location.Color = SelectedColor;
			location.AvailableForDeckAssignment = AvailableForDeckAssignment;
			location.AllowMultipleDecks = AllowMultipleDecks;
			await locationRepo.UpdateAsync(location);
			StatusMessage = "Settings saved.";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Save failed: {ex.Message}";
		}
	}

	[RelayCommand]
	private async Task GoBackAsync()
	{
		await Shell.Current.GoToAsync("..");
	}
}