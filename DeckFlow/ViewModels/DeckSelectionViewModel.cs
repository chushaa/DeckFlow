namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Core.Planning;
using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Services;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class DeckSelectionViewModel(
	IDeckRepository deckRepo,
	IDeckRequirementRepository requirementRepo,
	ILocationRepository locationRepo,
	IInventoryService inventoryService,
	IMovementPlanner planner) : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanConfirm))]
	private bool _hasSelection;

	[ObservableProperty]
	private bool _isGenerating;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isError;

	public bool CanConfirm => HasSelection && !IsGenerating;

	public ObservableCollection<DeckItemViewModel> AllDecks { get; } = [];

	public ObservableCollection<DeckItemViewModel> SelectedDecksInOrder { get; } = [];

	public async Task LoadAsync()
	{
		AllDecks.Clear();
		SelectedDecksInOrder.Clear();
		HasSelection = false;
		StatusMessage = string.Empty;
		IsError = false;

		var decks = await deckRepo.GetAllAsync();
		var locations = await locationRepo.GetAllAsync();
		var locationMap = locations.ToDictionary(l => l.Id, l => l.Name);

		foreach (var deck in decks.OrderBy(d => d.Name))
		{
			var requirements = await requirementRepo.GetByDeckIdAsync(deck.Id);
			var cardCount = requirements.Sum(r => r.Quantity);
			string? locationName = deck.BoundLocationId.HasValue && locationMap.TryGetValue(deck.BoundLocationId.Value, out var name)
				? name
				: null;

			AllDecks.Add(new DeckItemViewModel(deck.Id, deck.Name, locationName, cardCount));
		}
	}

	[RelayCommand]
	private void ToggleSelect(DeckItemViewModel deck)
	{
		if (deck.IsSelected)
		{
			deck.IsSelected = false;
			deck.OrderBadge = null;
			SelectedDecksInOrder.Remove(deck);
		}
		else
		{
			deck.IsSelected = true;
			SelectedDecksInOrder.Add(deck);
		}

		UpdateOrderBadges();
		HasSelection = SelectedDecksInOrder.Count > 0;
	}

	[RelayCommand]
	private void MoveUp(DeckItemViewModel deck)
	{
		var index = SelectedDecksInOrder.IndexOf(deck);
		if (index <= 0)
			return;

		SelectedDecksInOrder.Move(index, index - 1);
		UpdateOrderBadges();
	}

	[RelayCommand]
	private void MoveDown(DeckItemViewModel deck)
	{
		var index = SelectedDecksInOrder.IndexOf(deck);
		if (index < 0 || index >= SelectedDecksInOrder.Count - 1)
			return;

		SelectedDecksInOrder.Move(index, index + 1);
		UpdateOrderBadges();
	}

	[RelayCommand]
	private async Task ConfirmAsync()
	{
		if (SelectedDecksInOrder.Count == 0)
			return;

		try
		{
			IsGenerating = true;
			IsError = false;
			StatusMessage = "Generating movement plan...";
			OnPropertyChanged(nameof(CanConfirm));

			var selections = SelectedDecksInOrder
				.Select((d, i) => new DeckSelection(
					new Core.ValueObjects.DeckId(d.DeckId),
					i + 1))
				.ToList();

			var request = await inventoryService.BuildPlanRequestAsync(selections);
			var plan = planner.BuildPlan(request);

			await Shell.Current.GoToAsync("StepViewerPage", new Dictionary<string, object>
			{
				["Plan"] = plan
			});

			StatusMessage = string.Empty;
		}
		catch (Exception ex)
		{
			StatusMessage = $"Plan generation failed: {ex.Message}";
			IsError = true;
		}
		finally
		{
			IsGenerating = false;
			OnPropertyChanged(nameof(CanConfirm));
		}
	}

	[RelayCommand]
	private async Task GoBackAsync()
	{
		await Shell.Current.GoToAsync("..");
	}

	private void UpdateOrderBadges()
	{
		for (int i = 0; i < SelectedDecksInOrder.Count; i++)
			SelectedDecksInOrder[i].OrderBadge = i + 1;
	}
}