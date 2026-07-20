using DeckFlow.Data.Database;
using DeckFlow.Services;

namespace DeckFlow;

public partial class App : Application
{
	private readonly AppDatabase _database;

	public App(ThemeService themeService, AppDatabase database)
	{
		InitializeComponent();

		// ThemeService constructor loads persisted preference and applies it
		_ = themeService;
		_database = database;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	protected override async void OnStart()
	{
		await _database.EnsureInitializedAsync();
	}
}