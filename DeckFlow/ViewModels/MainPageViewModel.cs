namespace DeckFlow.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion - not applicable for this app

public partial class MainPageViewModel(IDeckRepository deckRepo, AppDatabase database) : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanBeginDeckFlow))]
	[NotifyPropertyChangedFor(nameof(DeckCountText))]
	private int _deckCount;

	public bool CanBeginDeckFlow => DeckCount > 0;

	public string DeckCountText => DeckCount switch
	{
		0 => "No decks imported yet",
		1 => "1 deck ready",
		_ => $"{DeckCount} decks ready"
	};

	public async Task LoadAsync()
	{
		await database.EnsureInitializedAsync();
		var decks = await deckRepo.GetAllAsync();
		DeckCount = decks.Count;
	}

	[RelayCommand]
	private async Task BeginDeckFlowAsync()
	{
		if (!CanBeginDeckFlow)
			return;

		await Shell.Current.GoToAsync("DeckSelectionPage");
	}

	[RelayCommand]
	private async Task CollectionAsync()
	{
		await Shell.Current.GoToAsync("CollectionsPage");
	}

	[RelayCommand]
	private async Task DecksAsync()
	{
		await Shell.Current.GoToAsync("DecksPage");
	}

	[RelayCommand]
	private async Task OpenSettingsAsync()
	{
		await Shell.Current.GoToAsync("SettingsPage");
	}
}