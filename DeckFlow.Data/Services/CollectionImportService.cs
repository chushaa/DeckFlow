namespace DeckFlow.Data.Services;

using DeckFlow.Core.Parsing;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Models;

public class CollectionImportService(
	ICardRepository cardRepo,
	IPrintingRepository printingRepo,
	ILocationRepository locationRepo,
	IOwnedCopyRepository ownedCopyRepo) : ICollectionImportService
{
	public Task<int> ImportAsync(string csvText, IProgress<(int Current, int Total)>? progress = null)
		=> ImportAsync(ManaBoxCsvParser.Parse(csvText), progress);

	public async Task<int> ImportAsync(IReadOnlyList<ParsedCollectionEntry> entries, IProgress<(int Current, int Total)>? progress = null)
	{
		// Phase 1: Pre-fetch all existing data (4 queries)
		var existingCards = await cardRepo.GetAllAsync();
		var existingPrintings = await printingRepo.GetAllAsync();
		var existingLocations = await locationRepo.GetAllAsync();
		var existingOwnedCopies = await ownedCopyRepo.GetAllAsync();

		var cardsByName = new Dictionary<string, CardModel>(StringComparer.OrdinalIgnoreCase);
		foreach (var card in existingCards)
			cardsByName[card.Name] = card;

		var knownScryfallIds = new HashSet<string>(
			existingPrintings.Select(p => p.ScryfallId), StringComparer.Ordinal);

		var locationsByName = new Dictionary<string, LocationModel>(StringComparer.OrdinalIgnoreCase);
		foreach (var loc in existingLocations)
			locationsByName[loc.Name] = loc;

		var ownedCopyLookup = new Dictionary<(Guid LocationId, string ScryfallId), OwnedCopyModel>();
		foreach (var oc in existingOwnedCopies)
			ownedCopyLookup[(oc.LocationId, oc.ScryfallId)] = oc;

		var preExistingOwnedCopyKeys = new HashSet<(Guid, string)>(ownedCopyLookup.Keys);
		var seenOwnedCopyKeys = new HashSet<(Guid, string)>();

		// Phase 2: In-memory processing (0 DB calls)
		var newCards = new List<CardModel>();
		var cardsToUpdate = new List<CardModel>();
		var newPrintings = new List<PrintingModel>();
		var newLocations = new List<LocationModel>();
		var newScryfallIds = new HashSet<string>(StringComparer.Ordinal);
		int importedCount = 0;
		int total = entries.Count;

		for (int i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];

			// Card resolution
			if (!cardsByName.TryGetValue(entry.CardName, out var card))
			{
				card = new CardModel
				{
					Id = Guid.CreateVersion7(),
					Name = entry.CardName,
					BackFaceName = entry.BackFaceName
				};
				cardsByName[entry.CardName] = card;
				newCards.Add(card);
			}
			else if (card.BackFaceName is null && entry.BackFaceName is not null)
			{
				card.BackFaceName = entry.BackFaceName;
				cardsToUpdate.Add(card);
			}

			// Printing resolution
			if (newScryfallIds.Add(entry.ScryfallId) && knownScryfallIds.Add(entry.ScryfallId))
			{
				newPrintings.Add(new PrintingModel
				{
					ScryfallId = entry.ScryfallId,
					CardId = card.Id,
					SetCode = entry.SetCode,
					SetName = entry.SetName,
					CollectorNumber = entry.CollectorNumber,
					IsFoil = entry.IsFoil
				});
			}

			// Location resolution
			if (!locationsByName.TryGetValue(entry.LocationName, out var location))
			{
				location = new LocationModel
				{
					Id = Guid.CreateVersion7(),
					Name = entry.LocationName
				};
				locationsByName[entry.LocationName] = location;
				newLocations.Add(location);
			}

			// Owned copy resolution (full sync)
			var ownedKey = (location.Id, entry.ScryfallId);
			if (ownedCopyLookup.TryGetValue(ownedKey, out var ownedCopy))
			{
				if (seenOwnedCopyKeys.Add(ownedKey))
					ownedCopy.Quantity = entry.Quantity;
				else
					ownedCopy.Quantity += entry.Quantity;
			}
			else
			{
				ownedCopy = new OwnedCopyModel
				{
					Id = Guid.CreateVersion7(),
					CardId = card.Id,
					ScryfallId = entry.ScryfallId,
					LocationId = location.Id,
					Quantity = entry.Quantity
				};
				ownedCopyLookup[ownedKey] = ownedCopy;
				seenOwnedCopyKeys.Add(ownedKey);
			}

			importedCount += entry.Quantity;
			progress?.Report((i + 1, total));
		}

		// Phase 3: Batch write (~7 operations)
		if (newCards.Count > 0)
			await cardRepo.InsertAllAsync(newCards);

		if (cardsToUpdate.Count > 0)
			await cardRepo.UpdateAllAsync(cardsToUpdate);

		if (newPrintings.Count > 0)
			await printingRepo.InsertAllAsync(newPrintings);

		if (newLocations.Count > 0)
			await locationRepo.InsertAllAsync(newLocations);

		var ownedCopiesToInsert = new List<OwnedCopyModel>();
		var ownedCopiesToUpdate = new List<OwnedCopyModel>();
		var ownedCopiesToDelete = new List<OwnedCopyModel>();

		foreach (var kvp in ownedCopyLookup)
		{
			if (preExistingOwnedCopyKeys.Contains(kvp.Key))
			{
				if (seenOwnedCopyKeys.Contains(kvp.Key))
					ownedCopiesToUpdate.Add(kvp.Value);
				else
					ownedCopiesToDelete.Add(kvp.Value);
			}
			else
			{
				ownedCopiesToInsert.Add(kvp.Value);
			}
		}

		if (ownedCopiesToInsert.Count > 0)
			await ownedCopyRepo.InsertAllAsync(ownedCopiesToInsert);

		if (ownedCopiesToUpdate.Count > 0)
			await ownedCopyRepo.UpdateAllAsync(ownedCopiesToUpdate);

		if (ownedCopiesToDelete.Count > 0)
			await ownedCopyRepo.DeleteAsync(ownedCopiesToDelete);

		return importedCount;
	}
}