namespace DeckFlow.Views;

using DeckFlow.Data.Abstractions;
using DeckFlow.Services;

public partial class SettingsPage : ContentPage
{
	private readonly ThemeService _themeService;
	private readonly IOwnedCopyRepository _ownedCopyRepo;

	public string SelectedTheme => _themeService.CurrentPreference.ToString();

	public SettingsPage(ThemeService themeService, IOwnedCopyRepository ownedCopyRepo)
	{
		_themeService = themeService;
		_ownedCopyRepo = ownedCopyRepo;
		BindingContext = this;
		InitializeComponent();
	}

	private void OnThemeCheckedChanged(object? sender, CheckedChangedEventArgs e)
	{
		if (!e.Value || sender is not RadioButton radio || radio.Value is not string value)
			return;

		if (Enum.TryParse<ThemePreference>(value, out var preference))
		{
			_themeService.SetTheme(preference);
			OnPropertyChanged(nameof(SelectedTheme));
		}
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}

	private async void OnDeleteCollectionClicked(object? sender, EventArgs e)
	{
		var confirm = await DisplayAlertAsync(
			"Delete Collection",
			"Are you sure you want to delete all owned card copies? This cannot be undone.",
			"Delete",
			"Cancel");

		if (!confirm)
			return;

		await _ownedCopyRepo.DeleteAllAsync();
		await DisplayAlertAsync("Collection Deleted", "All owned card copies have been removed.", "OK");
	}
}