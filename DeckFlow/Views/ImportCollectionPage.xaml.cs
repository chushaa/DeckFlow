namespace DeckFlow.Views;

using DeckFlow.ViewModels;

public partial class ImportCollectionPage : ContentPage
{
	public ImportCollectionPage(ImportCollectionViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}