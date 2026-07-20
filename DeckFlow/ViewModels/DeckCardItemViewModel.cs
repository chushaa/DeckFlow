namespace DeckFlow.ViewModels;

public class DeckCardItemViewModel(int quantity, string cardName, string? setCode, string? collectorNumber, bool? isFoil)
{
	public int Quantity { get; } = quantity;
	public string CardName { get; } = cardName;
	public string? SetCode { get; } = setCode;
	public string? CollectorNumber { get; } = collectorNumber;
	public bool? IsFoil { get; } = isFoil;
	public bool HasPrintingInfo => SetCode is not null;
	public bool ShowFoil => IsFoil == true;
	public string SetInfo => HasPrintingInfo ? $"{SetCode!.ToUpperInvariant()} #{CollectorNumber}" : string.Empty;
}