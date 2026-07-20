using DeckFlow.Core.Parsing;

namespace DeckFlow.Tests.Parsing;

public sealed class ManaBoxCsvParser_Tests
{
	private const string ValidHeader =
		"Name,Set code,Set name,Collector number,Foil,Quantity,Scryfall ID,Binder Name";

	[Fact]
	public void Parse_ValidRow_ReturnsCorrectEntry()
	{
		var csv = $"""
			{ValidHeader}
			Lightning Bolt,M21,Core Set 2021,199,,1,d4e50178-78d1-4b52-9a42-4273ac938442,My Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);

		var entry = Assert.Single(result);
		Assert.Equal("Lightning Bolt", entry.CardName);
		Assert.Equal("M21", entry.SetCode);
		Assert.Equal("Core Set 2021", entry.SetName);
		Assert.Equal("199", entry.CollectorNumber);
		Assert.False(entry.IsFoil);
		Assert.Equal(1, entry.Quantity);
		Assert.Equal("d4e50178-78d1-4b52-9a42-4273ac938442", entry.ScryfallId);
		Assert.Equal("My Binder", entry.LocationName);
	}

	[Fact]
	public void Parse_FoilEntry()
	{
		var csv = $"""
			{ValidHeader}
			Sol Ring,C21,Commander 2021,263,foil,1,9e91e848-d7d0-4a0c-bfba-7c0d3bb2bbad,Commander Staples
			""";

		var result = ManaBoxCsvParser.Parse(csv);

		var entry = Assert.Single(result);
		Assert.True(entry.IsFoil);
	}

	[Fact]
	public void Parse_MultipleRows()
	{
		var csv = $"""
			{ValidHeader}
			Lightning Bolt,M21,Core Set 2021,199,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder A
			Counterspell,MH2,Modern Horizons 2,267,,2,4d3e4d9c-1a7c-4d4c-b75a-1f3e4f5a6b7c,Binder B
			""";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.Equal(2, result.Count);
		Assert.Equal("Lightning Bolt", result[0].CardName);
		Assert.Equal("Counterspell", result[1].CardName);
	}

	[Fact]
	public void Parse_SkipsEmptyLines()
	{
		var csv = ValidHeader + "\nLightning Bolt,M21,Core Set 2021,199,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder\n\n";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.Single(result);
	}

	[Fact]
	public void Parse_SkipsRowsWithInvalidScryfallId()
	{
		var csv = $"""
			{ValidHeader}
			Bad Card,M21,Core Set 2021,199,,1,not-a-guid,Binder
			Good Card,M21,Core Set 2021,200,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.Single(result);
		Assert.Equal("Good Card", result[0].CardName);
	}

	[Fact]
	public void Parse_MissingRequiredColumn_ThrowsFormatException()
	{
		var csv = """
			Name,Set code,Collector number
			Lightning Bolt,M21,199
			""";

		Assert.Throws<FormatException>(() => ManaBoxCsvParser.Parse(csv));
	}

	[Fact]
	public void Parse_OnlyHeader_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() => ManaBoxCsvParser.Parse(ValidHeader));
	}

	[Fact]
	public void Parse_EmptyString_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() => ManaBoxCsvParser.Parse(""));
	}

	[Fact]
	public void Parse_QuotedFieldWithComma()
	{
		var csv = $"""
			{ValidHeader}
			"Jace, the Mind Sculptor",WWK,Worldwake,31,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);

		var entry = Assert.Single(result);
		Assert.Equal("Jace, the Mind Sculptor", entry.CardName);
	}

	[Fact]
	public void Parse_CollectorNumberWithStar_TrimsStar()
	{
		var csv = $"""
			{ValidHeader}
			Card Name,SET,Set Name,123★,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);

		var entry = Assert.Single(result);
		Assert.Equal("123", entry.CollectorNumber);
	}

	[Fact]
	public void Parse_CaseInsensitiveFoilField()
	{
		var csv = $"""
			{ValidHeader}
			Card,SET,Set Name,123,Foil,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.True(result[0].IsFoil);
	}

	[Fact]
	public void Parse_NonFoilEmpty_ReturnsFalse()
	{
		var csv = $"""
			{ValidHeader}
			Card,SET,Set Name,123,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.False(result[0].IsFoil);
	}

	[Fact]
	public void Parse_DfcWithDoubleSlash_NormalizesToFrontFace()
	{
		var csv = $"""
			{ValidHeader}
			"Aclazotz, Deepest Betrayal // Temple of the Dead",LCI,The Lost Caverns of Ixalan,88,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);

		var entry = Assert.Single(result);
		Assert.Equal("Aclazotz, Deepest Betrayal", entry.CardName);
		Assert.Equal("Temple of the Dead", entry.BackFaceName);
	}

	[Fact]
	public void Parse_SingleFacedCard_BackFaceNameIsNull()
	{
		var csv = $"""
			{ValidHeader}
			Lightning Bolt,M21,Core Set 2021,199,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);

		var entry = Assert.Single(result);
		Assert.Equal("Lightning Bolt", entry.CardName);
		Assert.Null(entry.BackFaceName);
	}

	[Fact]
	public void Parse_MultipleQuantity()
	{
		var csv = $"""
			{ValidHeader}
			Card,SET,Set Name,123,,4,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.Equal(4, result[0].Quantity);
	}

	[Fact]
	public void Parse_CaseInsensitiveHeaders()
	{
		var csv = """
			name,set code,set name,collector number,foil,quantity,scryfall id,binder name
			Card,SET,Set Name,123,,1,d4e50178-78d1-4b52-9a42-4273ac938442,Binder
			""";

		var result = ManaBoxCsvParser.Parse(csv);
		Assert.Single(result);
	}
}