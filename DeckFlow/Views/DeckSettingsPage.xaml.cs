namespace DeckFlow.Views;

using DeckFlow.ViewModels;

public partial class DeckSettingsPage : ContentPage
{
	private readonly DeckSettingsViewModel _viewModel;

	public DeckSettingsPage(DeckSettingsViewModel viewModel)
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