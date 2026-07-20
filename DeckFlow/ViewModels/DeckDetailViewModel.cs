namespace DeckFlow.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeckFlow.Data.Abstractions;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class DeckDetailViewModel(
	IDeckRepository deckRepo,
	IDeckRequirementRepository reqRepo,
	ICardRepository cardRepo,
	IPrintingRepository printingRepo,
	ILocationRepository locationRepo) : ObservableObject, IQueryAttributable
{
	private Guid _deckId;

	[ObservableProperty]
	private string _deckName = string.Empty;

	[ObservableProperty]
	private string? _boundLocationText;

	[ObservableProperty]
	private bool _hasBoundLocation;

	[ObservableProperty]
	private int _cardCount;

	[ObservableProperty]
	private bool _isLoading;

	public ObservableCollection<DeckCardItemViewModel> Cards { get; } = [];

	public string CardCountText => $"{CardCount} cards";

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("DeckId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id))
			_deckId = id;
	}

	public async Task LoadAsync()
	{
		IsLoading = true;

		try
		{
			var (deckName, locationText, items, totalCount) = await Task.Run(async () =>
			{
				var deckTask = deckRepo.GetByIdAsync(_deckId);
				var reqsTask = reqRepo.GetByDeckIdAsync(_deckId);
				await Task.WhenAll(deckTask, reqsTask);

				var deck = await deckTask;
				if (deck is null)
					return (string.Empty, (string?)null, new List<DeckCardItemViewModel>(), 0);

				var reqs = await reqsTask;

				// Resolve bound location name
				string? locText = null;
				if (deck.BoundLocationId.HasValue)
				{
					var loc = await locationRepo.GetByIdAsync(deck.BoundLocationId.Value);
					if (loc is not null)
						locText = loc.Name;
				}

				// Build lookup sets from requirement data
				var cardIds = reqs.Select(r => r.CardId).ToHashSet();
				var scryfallIds = reqs
					.Where(r => r.RequestedScryfallId is not null)
					.Select(r => r.RequestedScryfallId!)
					.ToHashSet();

				// Batch-load cards and printings
				var allCards = await cardRepo.GetAllAsync();
				var cardMap = new Dictionary<Guid, Data.Models.CardModel>(cardIds.Count);
				foreach (var c in allCards)
				{
					if (cardIds.Contains(c.Id))
						cardMap[c.Id] = c;
				}

				Dictionary<string, Data.Models.PrintingModel> printingMap;
				if (scryfallIds.Count > 0)
				{
					var allPrintings = await printingRepo.GetAllAsync();
					printingMap = new Dictionary<string, Data.Models.PrintingModel>(scryfallIds.Count);
					foreach (var p in allPrintings)
					{
						if (scryfallIds.Contains(p.ScryfallId))
							printingMap[p.ScryfallId] = p;
					}
				}
				else
				{
					printingMap = [];
				}

				// Build card items
				var cardItems = new List<DeckCardItemViewModel>(reqs.Count);
				var total = 0;

				foreach (var req in reqs)
				{
					var cardName = cardMap.TryGetValue(req.CardId, out var card) ? card.Name : "Unknown";

					string? setCode = null;
					string? collectorNumber = null;
					bool? isFoil = null;

					if (req.RequestedScryfallId is not null && printingMap.TryGetValue(req.RequestedScryfallId, out var printing))
					{
						setCode = printing.SetCode;
						collectorNumber = printing.CollectorNumber;
						isFoil = printing.IsFoil;
					}
					else if (req.RequestedSetCode is not null)
					{
						setCode = req.RequestedSetCode;
						collectorNumber = req.RequestedCollectorNumber;
					}

					cardItems.Add(new DeckCardItemViewModel(req.Quantity, cardName, setCode, collectorNumber, isFoil));
					total += req.Quantity;
				}

				cardItems.Sort((a, b) => string.Compare(a.CardName, b.CardName, StringComparison.Ordinal));

				return (deck.Name, locText, cardItems, total);
			});

			if (string.IsNullOrEmpty(deckName))
				return;

			DeckName = deckName;

			if (locationText is not null)
			{
				BoundLocationText = locationText;
				HasBoundLocation = true;
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
		await Shell.Current.GoToAsync($"DeckSettingsPage?DeckId={_deckId}");
	}
}