using DeckFlow.Core.Parsing;

namespace DeckFlow.Tests.Parsing;

public sealed class DecklistParser_Tests
{
	[Fact]
	public void Parse_SimpleEntry_ReturnsCorrectValues()
	{
		var result = DecklistParser.Parse("1 Lightning Bolt");

		var entry = Assert.Single(result);
		Assert.Equal(1, entry.Quantity);
		Assert.Equal("Lightning Bolt", entry.CardName);
		Assert.Null(entry.SetCode);
		Assert.Null(entry.CollectorNumber);
		Assert.False(entry.IsFoil);
		Assert.False(entry.IsEtched);
	}

	[Fact]
	public void Parse_MultipleQuantity()
	{
		var result = DecklistParser.Parse("4 Counterspell");

		var entry = Assert.Single(result);
		Assert.Equal(4, entry.Quantity);
		Assert.Equal("Counterspell", entry.CardName);
	}

	[Fact]
	public void Parse_WithSetCodeAndCollectorNumber()
	{
		var result = DecklistParser.Parse("1 Sol Ring (C21) 263");

		var entry = Assert.Single(result);
		Assert.Equal("Sol Ring", entry.CardName);
		Assert.Equal("C21", entry.SetCode);
		Assert.Equal("263", entry.CollectorNumber);
	}

	[Fact]
	public void Parse_FoilMarker()
	{
		var result = DecklistParser.Parse("1 Sol Ring (C21) 263 *F*");

		var entry = Assert.Single(result);
		Assert.True(entry.IsFoil);
		Assert.False(entry.IsEtched);
	}

	[Fact]
	public void Parse_EtchedMarker()
	{
		var result = DecklistParser.Parse("1 Sol Ring (C21) 263 *E*");

		var entry = Assert.Single(result);
		Assert.False(entry.IsFoil);
		Assert.True(entry.IsEtched);
	}

	[Fact]
	public void Parse_SkipsEmptyLines()
	{
		var text = """
			1 Lightning Bolt

			1 Counterspell
			""";

		var result = DecklistParser.Parse(text);
		Assert.Equal(2, result.Count);
	}

	[Fact]
	public void Parse_SkipsCommentLines()
	{
		var text = """
			# This is a comment
			1 Lightning Bolt
			// Another comment
			1 Counterspell
			""";

		var result = DecklistParser.Parse(text);
		Assert.Equal(2, result.Count);
	}

	[Fact]
	public void Parse_MultipleEntries()
	{
		var text = """
			4 Lightning Bolt
			4 Counterspell
			1 Sol Ring (C21) 263
			""";

		var result = DecklistParser.Parse(text);
		Assert.Equal(3, result.Count);
		Assert.Equal("Lightning Bolt", result[0].CardName);
		Assert.Equal("Counterspell", result[1].CardName);
		Assert.Equal("Sol Ring", result[2].CardName);
	}

	[Fact]
	public void Parse_InvalidLine_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() => DecklistParser.Parse("not a valid line"));
	}

	[Fact]
	public void Parse_EmptyString_ReturnsEmpty()
	{
		var result = DecklistParser.Parse("");
		Assert.Empty(result);
	}

	[Fact]
	public void Parse_OnlyComments_ReturnsEmpty()
	{
		var text = """
			# comment
			// another comment
			""";

		var result = DecklistParser.Parse(text);
		Assert.Empty(result);
	}

	[Fact]
	public void Parse_CardNameWithMultipleWords()
	{
		var result = DecklistParser.Parse("1 Toski, Bearer of Secrets");

		var entry = Assert.Single(result);
		Assert.Equal("Toski, Bearer of Secrets", entry.CardName);
	}

	[Fact]
	public void Parse_LowercaseFoilMarker()
	{
		var result = DecklistParser.Parse("1 Sol Ring (C21) 263 *f*");

		var entry = Assert.Single(result);
		Assert.True(entry.IsFoil);
	}

	[Fact]
	public void Parse_NoFoilMarker_BothFalse()
	{
		var result = DecklistParser.Parse("1 Lightning Bolt (M21) 199");

		var entry = Assert.Single(result);
		Assert.False(entry.IsFoil);
		Assert.False(entry.IsEtched);
	}

	[Fact]
	public void Parse_DfcWithSingleSlash_NormalizesToFrontFace()
	{
		var result = DecklistParser.Parse("1 Nicol Bolas, the Ravager / Nicol Bolas, the Arisen");

		var entry = Assert.Single(result);
		Assert.Equal("Nicol Bolas, the Ravager", entry.CardName);
		Assert.Equal("Nicol Bolas, the Arisen", entry.BackFaceName);
	}

	[Fact]
	public void Parse_SingleFacedCard_BackFaceNameIsNull()
	{
		var result = DecklistParser.Parse("1 Lightning Bolt");

		var entry = Assert.Single(result);
		Assert.Null(entry.BackFaceName);
	}
}