namespace DeckFlow.Views;

using DeckFlow.ViewModels;

public partial class DeckSelectionPage : ContentPage
{
	private readonly DeckSelectionViewModel _viewModel;

	public DeckSelectionPage(DeckSelectionViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadAsync();
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}