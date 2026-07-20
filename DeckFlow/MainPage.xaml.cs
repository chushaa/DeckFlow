namespace DeckFlow;

using DeckFlow.ViewModels;

public partial class MainPage : ContentPage
{
	private readonly MainPageViewModel _viewModel;
	private DateTime _lastBackPressedUtc;

	public MainPage(MainPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		_lastBackPressedUtc = DateTime.MinValue;
		await _viewModel.LoadAsync();
	}

	protected override bool OnBackButtonPressed()
	{
		var now = DateTime.UtcNow;

		if ((now - _lastBackPressedUtc).TotalSeconds <= 2)
		{
#if ANDROID
			Platform.CurrentActivity?.MoveTaskToBack(true);
#elif WINDOWS
			Application.Current?.Quit();
#endif
			return true;
		}

		_lastBackPressedUtc = now;
#if ANDROID
		Android.Widget.Toast.MakeText(Platform.CurrentActivity, "Press back again to exit", Android.Widget.ToastLength.Short)?.Show();
#endif
		return true;
	}
}