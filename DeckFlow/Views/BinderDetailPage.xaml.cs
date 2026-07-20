namespace DeckFlow.Views;

using DeckFlow.ViewModels;

public partial class BinderDetailPage : ContentPage
{
	private readonly BinderDetailViewModel _viewModel;

	public BinderDetailPage(BinderDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await Task.Delay(150);
		await _viewModel.LoadAsync();
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}