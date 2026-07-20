namespace DeckFlow.ViewModels;

public class BinderCardItemViewModel(int quantity, string cardName, string setCode, string collectorNumber, bool isFoil)
{
	public int Quantity { get; } = quantity;
	public string CardName { get; } = cardName;
	public string SetCode { get; } = setCode;
	public string CollectorNumber { get; } = collectorNumber;
	public bool IsFoil { get; } = isFoil;
	public string SetInfo => $"{SetCode.ToUpperInvariant()} #{CollectorNumber}";
}