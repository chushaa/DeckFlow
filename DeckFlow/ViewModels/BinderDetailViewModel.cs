namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Data.Abstractions;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class BinderDetailViewModel(
	ILocationRepository locationRepo,
	IOwnedCopyRepository ownedCopyRepo,
	ICardRepository cardRepo,
	IPrintingRepository printingRepo,
	IDeckRepository deckRepo) : ObservableObject, IQueryAttributable
{
	private Guid _locationId;

	[ObservableProperty]
	private string _binderName = string.Empty;

	[ObservableProperty]
	private string _binderColor = "#2D8A96";

	[ObservableProperty]
	private string? _assignedDecksText;

	[ObservableProperty]
	private bool _hasAssignedDecks;

	[ObservableProperty]
	private int _cardCount;

	[ObservableProperty]
	private bool _isLoading;

	public ObservableCollection<BinderCardItemViewModel> Cards { get; } = [];

	public string CardCountText => $"{CardCount} cards";

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("LocationId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id))
			_locationId = id;
	}

	public async Task LoadAsync()
	{
		IsLoading = true;

		try
		{
			var (locationName, locationColor, assignedDeckNames, items, totalCount) = await Task.Run(async () =>
			{
				var locationTask = locationRepo.GetByIdAsync(_locationId);
				var decksTask = deckRepo.GetAllAsync();
				var copiesTask = ownedCopyRepo.GetByLocationIdAsync(_locationId);
				await Task.WhenAll(locationTask, decksTask, copiesTask);

				var loc = await locationTask;
				if (loc is null)
					return (string.Empty, string.Empty, new List<string>(), new List<BinderCardItemViewModel>(), 0);

				var allDecks = await decksTask;
				var copies = await copiesTask;

				// Assigned decks
				var deckNames = allDecks
					.Where(d => d.BoundLocationId == _locationId)
					.Select(d => d.Name)
					.OrderBy(n => n)
					.ToList();

				// Build lookup maps from only the IDs we need
				var printingIds = copies.Select(c => c.ScryfallId).ToHashSet();
				var cardIds = copies.Select(c => c.CardId).ToHashSet();

				var allPrintings = await printingRepo.GetAllAsync();
				var printingMap = new Dictionary<string, Data.Models.PrintingModel>(printingIds.Count);
				foreach (var p in allPrintings)
				{
					if (printingIds.Contains(p.ScryfallId))
						printingMap[p.ScryfallId] = p;
				}

				var allCards = await cardRepo.GetAllAsync();
				var cardMap = new Dictionary<Guid, Data.Models.CardModel>(cardIds.Count);
				foreach (var c in allCards)
				{
					if (cardIds.Contains(c.Id))
						cardMap[c.Id] = c;
				}

				// Build card items
				var cardItems = new List<BinderCardItemViewModel>(copies.Count);
				var total = 0;

				foreach (var copy in copies)
				{
					var cardName = cardMap.TryGetValue(copy.CardId, out var card) ? card.Name : "Unknown";
					var setCode = string.Empty;
					var collectorNumber = string.Empty;
					var isFoil = false;

					if (printingMap.TryGetValue(copy.ScryfallId, out var printing))
					{
						setCode = printing.SetCode;
						collectorNumber = printing.CollectorNumber;
						isFoil = printing.IsFoil;
					}

					cardItems.Add(new BinderCardItemViewModel(copy.Quantity, cardName, setCode, collectorNumber, isFoil));
					total += copy.Quantity;
				}

				cardItems.Sort((a, b) =>
				{
					var cmp = string.Compare(a.CardName, b.CardName, StringComparison.Ordinal);
					return cmp != 0 ? cmp : string.Compare(a.SetCode, b.SetCode, StringComparison.Ordinal);
				});

				return (loc.Name, loc.Color, deckNames, cardItems, total);
			});

			if (string.IsNullOrEmpty(locationName))
				return;

			BinderName = locationName;
			BinderColor = locationColor;

			if (assignedDeckNames.Count > 0)
			{
				AssignedDecksText = string.Join(" + ", assignedDeckNames);
				HasAssignedDecks = true;
			}

			Cards.Clear();
			foreach (var item in items)
				Cards.Add(item);

			CardCount = totalCount;
			OnPropertyChanged(nameof(CardCountText));
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task OpenSettingsAsync()
	{
		await Shell.Current.GoToAsync($"BinderSettingsPage?LocationId={_locationId}");
	}
}