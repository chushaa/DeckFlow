namespace DeckFlow.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

#pragma warning disable MVVMTK0045 // WinRT AOT partial property suggestion

public partial class DeckItemViewModel : ObservableObject
{
	public Guid DeckId { get; }

	public string Name { get; }

	public string BoundLocationName { get; }

	public int CardCount { get; }

	[ObservableProperty]
	private bool _isSelected;

	[ObservableProperty]
	private int? _orderBadge;

	public DeckItemViewModel(Guid deckId, string name, string? boundLocationName, int cardCount)
	{
		DeckId = deckId;
		Name = name;
		BoundLocationName = boundLocationName ?? "Unbound";
		CardCount = cardCount;
	}
}