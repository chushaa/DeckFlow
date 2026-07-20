namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Data.Abstractions;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class CollectionsViewModel(
	ILocationRepository locationRepo,
	IOwnedCopyRepository ownedCopyRepo,
	IDeckRepository deckRepo) : ObservableObject
{
	[ObservableProperty]
	private bool _isLoading;

	public ObservableCollection<BinderTileViewModel> Binders { get; } = [];

	public string BinderCountText => $"Binders: {Binders.Count}";

	public async Task LoadAsync()
	{
		IsLoading = true;

		try
		{
			var locations = await locationRepo.GetAllAsync();

			if (locations.Count == 0)
			{
				Binders.Clear();
				OnPropertyChanged(nameof(BinderCountText));

				var goImport = await Shell.Current.DisplayAlertAsync(
					"No Binders",
					"You haven't imported a collection yet. Would you like to import one now?",
					"Import Collection",
					"Cancel");

				if (goImport)
					await Shell.Current.GoToAsync("ImportCollectionPage");

				return;
			}

			var tiles = await Task.Run(async () =>
			{
				var copyCountByLocation = await ownedCopyRepo.GetCardCountsByLocationAsync();
				var allDecks = await deckRepo.GetAllAsync();

				var deckNamesByLocation = allDecks
					.Where(d => d.BoundLocationId.HasValue)
					.GroupBy(d => d.BoundLocationId!.Value)
					.ToDictionary(g => g.Key, g => string.Join(" + ", g.Select(d => d.Name).OrderBy(n => n)));

				return locations
					.OrderBy(l => l.Name)
					.Select(l =>
					{
						copyCountByLocation.TryGetValue(l.Id, out var count);
						deckNamesByLocation.TryGetValue(l.Id, out var deckNames);
						return new BinderTileViewModel(l.Id, l.Name, l.Color, count, deckNames);
					})
					.ToList();
			});

			Binders.Clear();
			foreach (var tile in tiles)
				Binders.Add(tile);
			OnPropertyChanged(nameof(BinderCountText));
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task OpenBinderAsync(BinderTileViewModel tile)
	{
		await Shell.Current.GoToAsync($"BinderDetailPage?LocationId={tile.LocationId}");
	}

	[RelayCommand]
	private async Task OpenBinderSettingsAsync(BinderTileViewModel tile)
	{
		await Shell.Current.GoToAsync($"BinderSettingsPage?LocationId={tile.LocationId}");
	}

	[RelayCommand]
	private async Task ImportCollectionAsync()
	{
		await Shell.Current.GoToAsync("ImportCollectionPage");
	}
}