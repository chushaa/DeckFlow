using DeckFlow.Core.Parsing;

namespace DeckFlow.Tests.Parsing;

public sealed class CardNameHelper_Tests
{
	[Fact]
	public void SplitCardName_SingleFacedCard_ReturnsNameAndNull()
	{
		var (front, back) = CardNameHelper.SplitCardName("Lightning Bolt");

		Assert.Equal("Lightning Bolt", front);
		Assert.Null(back);
	}

	[Fact]
	public void SplitCardName_CardWithComma_ReturnsNameAndNull()
	{
		var (front, back) = CardNameHelper.SplitCardName("Toski, Bearer of Secrets");

		Assert.Equal("Toski, Bearer of Secrets", front);
		Assert.Null(back);
	}

	[Fact]
	public void SplitCardName_DoubleSlash_SplitsCorrectly()
	{
		var (front, back) = CardNameHelper.SplitCardName("Aclazotz, Deepest Betrayal // Temple of the Dead");

		Assert.Equal("Aclazotz, Deepest Betrayal", front);
		Assert.Equal("Temple of the Dead", back);
	}

	[Fact]
	public void SplitCardName_SingleSlash_SplitsCorrectly()
	{
		var (front, back) = CardNameHelper.SplitCardName("Nicol Bolas, the Ravager / Nicol Bolas, the Arisen");

		Assert.Equal("Nicol Bolas, the Ravager", front);
		Assert.Equal("Nicol Bolas, the Arisen", back);
	}

	[Fact]
	public void SplitCardName_SplitCard_DoubleSlash()
	{
		var (front, back) = CardNameHelper.SplitCardName("Wear // Tear");

		Assert.Equal("Wear", front);
		Assert.Equal("Tear", back);
	}

	[Fact]
	public void SplitCardName_AdventureCard_SingleSlash()
	{
		var (front, back) = CardNameHelper.SplitCardName("Monster Manual / Zoological Study");

		Assert.Equal("Monster Manual", front);
		Assert.Equal("Zoological Study", back);
	}

	[Fact]
	public void SplitCardName_DoubleSlashTakesPriority()
	{
		// If both separators existed, double-slash wins (unlikely in practice)
		var (front, back) = CardNameHelper.SplitCardName("Front // Back / Extra");

		Assert.Equal("Front", front);
		Assert.Equal("Back / Extra", back);
	}

	[Fact]
	public void SplitCardName_TrimsParts()
	{
		var (front, back) = CardNameHelper.SplitCardName("  Front  //  Back  ");

		Assert.Equal("Front", front);
		Assert.Equal("Back", back);
	}

	[Fact]
	public void JoinCardName_WithBackFace_ReturnsDoubleSlashFormat()
	{
		var result = CardNameHelper.JoinCardName("Nicol Bolas, the Ravager", "Nicol Bolas, the Arisen");

		Assert.Equal("Nicol Bolas, the Ravager // Nicol Bolas, the Arisen", result);
	}

	[Fact]
	public void JoinCardName_WithoutBackFace_ReturnsFrontOnly()
	{
		var result = CardNameHelper.JoinCardName("Lightning Bolt", null);

		Assert.Equal("Lightning Bolt", result);
	}

	[Fact]
	public void SplitCardName_RealDfc_NicolBolas()
	{
		// ManaBox format
		var (front1, back1) = CardNameHelper.SplitCardName("Nicol Bolas, the Ravager // Nicol Bolas, the Arisen");
		Assert.Equal("Nicol Bolas, the Ravager", front1);
		Assert.Equal("Nicol Bolas, the Arisen", back1);

		// Deck list format
		var (front2, back2) = CardNameHelper.SplitCardName("Nicol Bolas, the Ravager / Nicol Bolas, the Arisen");
		Assert.Equal("Nicol Bolas, the Ravager", front2);
		Assert.Equal("Nicol Bolas, the Arisen", back2);

		// Both normalize to the same front face
		Assert.Equal(front1, front2);
	}
}