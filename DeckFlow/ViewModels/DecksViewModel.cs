namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Data.Abstractions;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class DecksViewModel(
	IDeckRepository deckRepo,
	IDeckRequirementRepository reqRepo,
	ILocationRepository locationRepo) : ObservableObject
{
	[ObservableProperty]
	private bool _isLoading;

	public ObservableCollection<DeckTileViewModel> Decks { get; } = [];

	public string DeckCountText => $"Decks: {Decks.Count}";

	public async Task LoadAsync()
	{
		IsLoading = true;

		try
		{
			var decks = await deckRepo.GetAllAsync();
			var locations = await locationRepo.GetAllAsync();
			var locationMap = locations.ToDictionary(l => l.Id, l => l.Name);

			var tiles = new List<DeckTileViewModel>();

			foreach (var deck in decks.OrderBy(d => d.Name))
			{
				var reqs = await reqRepo.GetByDeckIdAsync(deck.Id);
				var cardCount = reqs.Sum(r => r.Quantity);
				var locationName = deck.BoundLocationId.HasValue && locationMap.TryGetValue(deck.BoundLocationId.Value, out var name)
					? name
					: "Unbound";

				tiles.Add(new DeckTileViewModel(deck.Id, deck.Name, cardCount, locationName));
			}

			Decks.Clear();
			foreach (var tile in tiles)
				Decks.Add(tile);
			OnPropertyChanged(nameof(DeckCountText));

			if (Decks.Count == 0)
			{
				var goImport = await Shell.Current.DisplayAlertAsync(
					"No Decks",
					"You haven't imported any decks yet. Would you like to import one now?",
					"Import Deck",
					"Cancel");

				if (goImport)
					await Shell.Current.GoToAsync("ImportDeckPage");
			}
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task OpenDeckDetailAsync(DeckTileViewModel tile)
	{
		await Shell.Current.GoToAsync($"DeckDetailPage?DeckId={tile.DeckId}");
	}

	[RelayCommand]
	private async Task OpenDeckSettingsAsync(DeckTileViewModel tile)
	{
		await Shell.Current.GoToAsync($"DeckSettingsPage?DeckId={tile.DeckId}");
	}

	[RelayCommand]
	private async Task ImportDeckAsync()
	{
		await Shell.Current.GoToAsync("ImportDeckPage");
	}

}