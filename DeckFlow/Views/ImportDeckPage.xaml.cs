namespace DeckFlow.Views;

using DeckFlow.ViewModels;

public partial class ImportDeckPage : ContentPage
{
	private readonly ImportDeckViewModel _viewModel;

	public ImportDeckPage(ImportDeckViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadLocationsAsync();
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}