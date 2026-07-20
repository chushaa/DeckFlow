namespace DeckFlow.Data.Services;

using DeckFlow.Core.Parsing;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Models;

public class DeckImportService(
	ICardRepository cardRepo,
	IPrintingRepository printingRepo,
	IDeckRepository deckRepo,
	IDeckRequirementRepository reqRepo) : IDeckImportService
{
	public async Task<Guid> ImportAsync(string decklistText, string deckName, Guid? boundLocationId)
	{
		var deck = new DeckModel
		{
			Id = Guid.CreateVersion7(),
			Name = deckName,
			BoundLocationId = boundLocationId
		};
		await deckRepo.InsertAsync(deck);

		var requirements = await BuildRequirementsAsync(deck.Id, decklistText);
		await reqRepo.InsertAllAsync(requirements);

		return deck.Id;
	}

	public async Task ReplaceRequirementsAsync(Guid deckId, string decklistText)
	{
		await reqRepo.DeleteByDeckIdAsync(deckId);
		var requirements = await BuildRequirementsAsync(deckId, decklistText);
		await reqRepo.InsertAllAsync(requirements);
	}

	private async Task<List<DeckRequirementModel>> BuildRequirementsAsync(Guid deckId, string decklistText)
	{
		var entries = DecklistParser.Parse(decklistText);
		var requirements = new List<DeckRequirementModel>();

		foreach (var entry in entries)
		{
			var card = await cardRepo.GetByNameAsync(entry.CardName);
			if (card is null)
			{
				card = new CardModel
				{
					Id = Guid.CreateVersion7(),
					Name = entry.CardName,
					BackFaceName = entry.BackFaceName
				};
				await cardRepo.InsertAsync(card);
			}
			else if (card.BackFaceName is null && entry.BackFaceName is not null)
			{
				card.BackFaceName = entry.BackFaceName;
				await cardRepo.UpdateAsync(card);
			}

			string? requestedScryfallId = null;
			if (entry.SetCode is not null && entry.CollectorNumber is not null)
			{
				var printings = await printingRepo.GetByCardIdAsync(card.Id);
				var match = printings.FirstOrDefault(p =>
					p.SetCode.Equals(entry.SetCode, StringComparison.OrdinalIgnoreCase) &&
					p.CollectorNumber == entry.CollectorNumber);
				requestedScryfallId = match?.ScryfallId;
			}

			requirements.Add(new DeckRequirementModel
			{
				Id = Guid.CreateVersion7(),
				DeckId = deckId,
				CardId = card.Id,
				Quantity = entry.Quantity,
				RequestedScryfallId = requestedScryfallId,
				RequestedSetCode = entry.SetCode,
				RequestedCollectorNumber = entry.CollectorNumber
			});
		}

		return requirements;
	}
}