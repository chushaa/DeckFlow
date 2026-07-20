namespace DeckFlow.Views;

using DeckFlow.ViewModels;

public partial class StepViewerPage : ContentPage
{
	public StepViewerPage(StepViewerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}