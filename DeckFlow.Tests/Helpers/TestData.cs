using DeckFlow.Core.Planning;
using DeckFlow.Core.Planning.Contracts;
using DeckFlow.Core.ValueObjects;

namespace DeckFlow.Tests.Helpers;

internal static class TestData
{
	internal sealed record IdBag(
		DeckId DeckA,
		DeckId DeckB,
		DeckId DeckC,
		LocationId Green,
		LocationId DeckALoc,
		LocationId DeckBLoc,
		LocationId DeckCLoc);

	/// <summary>
	/// Single deck (Deck A) needs Explore. 1 Explore in Green binder (unbound).
	/// Expected: PreGame pulls from Green, FinalReturn sends back to Green.
	/// </summary>
	public static (PlanRequest Req, IdBag Ids) BuildScenario1()
	{
		var ids = BaseIds();

		var explore = Card("Explore");
		var explorePrint = Printing("scryfall-explore-1");

		var decks = new[]
		{
			new DeckDefinition(ids.DeckA, "Deck A", ids.DeckALoc, new[]
			{
				new DeckRequirement(explore, 1, RequestedPrinting: null)
			})
		};

		var locations = BaseLocations(ids);

		var inv = new InventorySnapshot(new[]
		{
			new InventoryStack(ids.Green, explore, explorePrint, 1)
		});

		var req = BaseRequest(ids, selected: [(ids.DeckA, 1)], decks, locations, inv);

		return (req, ids);
	}

	/// <summary>
	/// Single deck (Deck B) needs Shock. 1 Shock in Deck C (unselected deck location).
	/// Expected: PreGame pulls from Deck C, FinalReturn sends back to Deck C.
	/// </summary>
	public static (PlanRequest Req, IdBag Ids) BuildScenario2()
	{
		var ids = BaseIds();

		var shock = Card("Shock");
		var shockPrint = Printing("scryfall-shock-1");

		var decks = new[]
		{
			new DeckDefinition(ids.DeckB, "Deck B", ids.DeckBLoc, new[]
			{
				new DeckRequirement(shock, 1, RequestedPrinting: null)
			}),
			new DeckDefinition(ids.DeckC, "Deck C", ids.DeckCLoc, Array.Empty<DeckRequirement>())
		};

		var locations = BaseLocations(ids);

		var inv = new InventorySnapshot(new[]
		{
			new InventoryStack(ids.DeckCLoc, shock, shockPrint, 1)
		});

		var req = BaseRequest(ids, selected: [(ids.DeckB, 1)], decks, locations, inv);

		return (req, ids);
	}

	/// <summary>
	/// Two decks: A then B. Deck A needs Explore + Fury. Deck B needs Shock + Fury.
	/// Fury starts in Deck B location. Order: A→B.
	/// Expected: PreGame pulls Explore from Green + Fury from Deck B.
	/// Transition returns Fury to Deck B (it's home), pulls Shock from Deck C.
	/// FinalReturn: Explore→Green, Shock→Deck C. Fury NOT mentioned (already home).
	/// </summary>
	public static (PlanRequest Req, IdBag Ids) BuildScenario3_AThenB()
	{
		var ids = BaseIds();

		var explore = Card("Explore");
		var fury = Card("Fury");
		var shock = Card("Shock");

		var explorePrint = Printing("scryfall-explore-1");
		var furyPrint = Printing("scryfall-fury-1");
		var shockPrint = Printing("scryfall-shock-1");

		var decks = new[]
		{
			new DeckDefinition(ids.DeckA, "Deck A", ids.DeckALoc, new[]
			{
				new DeckRequirement(explore, 1, null),
				new DeckRequirement(fury, 1, null)
			}),
			new DeckDefinition(ids.DeckB, "Deck B", ids.DeckBLoc, new[]
			{
				new DeckRequirement(shock, 1, null),
				new DeckRequirement(fury, 1, null)
			}),
			new DeckDefinition(ids.DeckC, "Deck C", ids.DeckCLoc, Array.Empty<DeckRequirement>())
		};

		var locations = BaseLocations(ids);

		var inv = new InventorySnapshot(new[]
		{
			new InventoryStack(ids.Green, explore, explorePrint, 1),
			new InventoryStack(ids.DeckBLoc, fury, furyPrint, 1),
			new InventoryStack(ids.DeckCLoc, shock, shockPrint, 1)
		});

		var req = BaseRequest(ids,
			selected: [(ids.DeckA, 1), (ids.DeckB, 2)],
			decks, locations, inv);

		return (req, ids);
	}

	/// <summary>
	/// Same setup as Scenario3, but order is B→A.
	/// Expected: PreGame pulls Shock from Deck C (Fury already in Deck B).
	/// Transition: move Fury from Deck B→Deck A, pull Explore from Green.
	/// FinalReturn: Explore→Green, Shock→Deck C, Fury→Deck B.
	/// </summary>
	public static (PlanRequest Req, IdBag Ids) BuildScenario4_BThenA()
	{
		var ids = BaseIds();

		var explore = Card("Explore");
		var fury = Card("Fury");
		var shock = Card("Shock");

		var explorePrint = Printing("scryfall-explore-1");
		var furyPrint = Printing("scryfall-fury-1");
		var shockPrint = Printing("scryfall-shock-1");

		var decks = new[]
		{
			new DeckDefinition(ids.DeckA, "Deck A", ids.DeckALoc, new[]
			{
				new DeckRequirement(explore, 1, null),
				new DeckRequirement(fury, 1, null)
			}),
			new DeckDefinition(ids.DeckB, "Deck B", ids.DeckBLoc, new[]
			{
				new DeckRequirement(shock, 1, null),
				new DeckRequirement(fury, 1, null)
			}),
			new DeckDefinition(ids.DeckC, "Deck C", ids.DeckCLoc, Array.Empty<DeckRequirement>())
		};

		var locations = BaseLocations(ids);

		var inv = new InventorySnapshot(new[]
		{
			new InventoryStack(ids.Green, explore, explorePrint, 1),
			new InventoryStack(ids.DeckBLoc, fury, furyPrint, 1),
			new InventoryStack(ids.DeckCLoc, shock, shockPrint, 1)
		});

		var req = BaseRequest(ids,
			selected: [(ids.DeckB, 1), (ids.DeckA, 2)],
			decks, locations, inv);

		return (req, ids);
	}

	/// <summary>
	/// Deck A needs 3x Opt. Binder A has 2, Binder B has 1.
	/// Expected: Fully consume Binder A (2x Opt) before touching Binder B (1x Opt).
	/// </summary>
	public static (PlanRequest Req, IdBag Ids) BuildGuardrail_ConsumeFully()
	{
		var ids = BaseIds();

		var opt = Card("Opt");
		var optPrint = Printing("scryfall-opt-1");

		var binderA = new LocationDefinition(new LocationId(Guid.CreateVersion7()), "Binder A", SourceLocationKind.UnboundCollection, null);
		var binderB = new LocationDefinition(new LocationId(Guid.CreateVersion7()), "Binder B", SourceLocationKind.UnboundCollection, null);

		var decks = new[]
		{
			new DeckDefinition(ids.DeckA, "Deck A", ids.DeckALoc, new[]
			{
				new DeckRequirement(opt, 3, null)
			})
		};

		var locations = BaseLocations(ids).Concat(new[] { binderA, binderB }).ToArray();

		var inv = new InventorySnapshot(new[]
		{
			new InventoryStack(binderA.Id, opt, optPrint, 2),
			new InventoryStack(binderB.Id, opt, optPrint, 1)
		});

		var req = BaseRequest(ids, selected: [(ids.DeckA, 1)], decks, locations, inv);

		return (req, ids);
	}

	#region Helpers

	private static IdBag BaseIds()
	{
		return new IdBag(
			DeckA: new DeckId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
			DeckB: new DeckId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
			DeckC: new DeckId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
			Green: new LocationId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
			DeckALoc: new LocationId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
			DeckBLoc: new LocationId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
			DeckCLoc: new LocationId(Guid.Parse("44444444-4444-4444-4444-444444444444")));
	}

	private static LocationDefinition[] BaseLocations(IdBag ids)
	{
		return
		[
			new LocationDefinition(ids.Green, "Green", SourceLocationKind.UnboundCollection, null),
			new LocationDefinition(ids.DeckALoc, "Deck A", SourceLocationKind.DeckBound, ids.DeckA),
			new LocationDefinition(ids.DeckBLoc, "Deck B", SourceLocationKind.DeckBound, ids.DeckB),
			new LocationDefinition(ids.DeckCLoc, "Deck C", SourceLocationKind.DeckBound, ids.DeckC),
		];
	}

	private static PlanRequest BaseRequest(
		IdBag ids,
		IEnumerable<(DeckId DeckId, int Order)> selected,
		IReadOnlyList<DeckDefinition> decks,
		IReadOnlyList<LocationDefinition> locations,
		InventorySnapshot inv)
	{
		return new PlanRequest(
			RequestedUtc: DateTime.UtcNow,
			SelectedDecksInOrder: selected.Select(x => new DeckSelection(x.DeckId, x.Order)).OrderBy(x => x.OrderIndex).ToArray(),
			AllDecks: decks,
			Locations: locations,
			Inventory: inv,
			Options: new PlanOptions());
	}

	internal static CardRef Card(string name)
		=> new(new CardId(Guid.CreateVersion7()), name);

	internal static PrintingRef Printing(string scryfallId, string? setCode = null, string? collector = null, bool foil = false)
		=> new(new PrintingId(scryfallId), setCode, collector, foil);

	#endregion Helpers
}