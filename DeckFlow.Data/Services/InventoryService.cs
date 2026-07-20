namespace DeckFlow.Data.Services;

using DeckFlow.Core.Parsing;
using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;
using DeckFlow.Data.Abstractions;

public class InventoryService(
	IDeckRepository deckRepo,
	IDeckRequirementRepository reqRepo,
	ILocationRepository locationRepo,
	ICardRepository cardRepo,
	IPrintingRepository printingRepo,
	IOwnedCopyRepository ownedCopyRepo) : IInventoryService
{
	public async Task<PlanRequest> BuildPlanRequestAsync(IReadOnlyList<DeckSelection> selectedDecks)
	{
		var allDeckModels = await deckRepo.GetAllAsync();
		var allLocations = await locationRepo.GetAllAsync();
		var allCards = await cardRepo.GetAllAsync();
		var allPrintings = await printingRepo.GetAllAsync();
		var allOwnedCopies = await ownedCopyRepo.GetAllAsync();

		var cardLookup = allCards.ToDictionary(c => c.Id);
		var printingLookup = allPrintings.ToDictionary(p => p.ScryfallId);
		var locationLookup = allLocations.ToDictionary(l => l.Id);

		var selectedDeckIds = selectedDecks.Select(s => s.DeckId.Value).ToHashSet();

		var deckDefinitions = new List<DeckDefinition>();
		foreach (var deck in allDeckModels)
		{
			var reqModels = await reqRepo.GetByDeckIdAsync(deck.Id);
			var requirements = reqModels.Select(r =>
			{
				var card = cardLookup[r.CardId];
				PrintingRef? requestedPrinting = null;
				if (r.RequestedScryfallId is not null)
				{
					requestedPrinting = new PrintingRef(
						new PrintingId(r.RequestedScryfallId),
						r.RequestedSetCode,
						r.RequestedCollectorNumber,
						false);
				}

				return new DeckRequirement(
					new CardRef(new CardId(card.Id), CardNameHelper.JoinCardName(card.Name, card.BackFaceName)),
					r.Quantity,
					requestedPrinting);
			}).ToList();

			deckDefinitions.Add(new DeckDefinition(
				new DeckId(deck.Id),
				deck.Name,
				deck.BoundLocationId.HasValue ? new LocationId(deck.BoundLocationId.Value) : null,
				requirements));
		}

		var locationDefinitions = allLocations.Select(l =>
		{
			var boundDeck = allDeckModels.FirstOrDefault(d => d.BoundLocationId == l.Id);
			var kind = boundDeck is not null
				? SourceLocationKind.DeckBound
				: SourceLocationKind.UnboundCollection;

			return new LocationDefinition(
				new LocationId(l.Id),
				l.Name,
				kind,
				boundDeck is not null ? new DeckId(boundDeck.Id) : null);
		}).ToList();

		var stacks = allOwnedCopies.Select(o =>
		{
			var card = cardLookup[o.CardId];
			var printing = printingLookup[o.ScryfallId];

			return new InventoryStack(
				new LocationId(o.LocationId),
				new CardRef(new CardId(card.Id), CardNameHelper.JoinCardName(card.Name, card.BackFaceName)),
				new PrintingRef(
					new PrintingId(printing.ScryfallId),
					printing.SetCode,
					printing.CollectorNumber,
					printing.IsFoil),
				o.Quantity);
		}).ToList();

		return new PlanRequest(
			DateTime.UtcNow,
			selectedDecks,
			deckDefinitions,
			locationDefinitions,
			new InventorySnapshot(stacks),
			new PlanOptions());
	}
}