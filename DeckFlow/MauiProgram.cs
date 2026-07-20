using DeckFlow.Core.Planning;
using DeckFlow.Data.Abstractions;
using DeckFlow.Data.Database;
using DeckFlow.Data.Repositories;
using DeckFlow.Data.Services;
using DeckFlow.Services;
using DeckFlow.ViewModels;
using DeckFlow.Views;
using Microsoft.Extensions.Logging;

namespace DeckFlow;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Inter-Regular.ttf", "InterRegular");
				fonts.AddFont("Inter-Bold.ttf", "InterBold");
				fonts.AddFont("Inter-Italic.ttf", "InterItalic");
				fonts.AddFont("Inter-BoldItalic.ttf", "InterBoldItalic");
			});

		// Database
		builder.Services.AddSingleton(_ =>
			new AppDatabase(Path.Combine(FileSystem.AppDataDirectory, "deckflow.db")));

		// Repositories
		builder.Services.AddSingleton<ICardRepository, CardRepository>();
		builder.Services.AddSingleton<IPrintingRepository, PrintingRepository>();
		builder.Services.AddSingleton<ILocationRepository, LocationRepository>();
		builder.Services.AddSingleton<IOwnedCopyRepository, OwnedCopyRepository>();
		builder.Services.AddSingleton<IDeckRepository, DeckRepository>();
		builder.Services.AddSingleton<IDeckRequirementRepository, DeckRequirementRepository>();

		// Data services
		builder.Services.AddTransient<ICollectionImportService, CollectionImportService>();
		builder.Services.AddTransient<IDeckImportService, DeckImportService>();
		builder.Services.AddTransient<IInventoryService, InventoryService>();

		// Core services
		builder.Services.AddSingleton<IMovementPlanner, MovementPlanner>();

		// App services
		builder.Services.AddSingleton<ThemeService>();

		// ViewModels
		builder.Services.AddTransient<MainPageViewModel>();
		builder.Services.AddTransient<ImportCollectionViewModel>();
		builder.Services.AddTransient<ImportDeckViewModel>();
		builder.Services.AddTransient<DecksViewModel>();
		builder.Services.AddTransient<DeckSettingsViewModel>();
		builder.Services.AddTransient<DeckDetailViewModel>();
		builder.Services.AddTransient<DeckSelectionViewModel>();
		builder.Services.AddTransient<StepViewerViewModel>();
		builder.Services.AddTransient<CollectionsViewModel>();
		builder.Services.AddTransient<BinderSettingsViewModel>();
		builder.Services.AddTransient<BinderDetailViewModel>();

		// Pages
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<ImportCollectionPage>();
		builder.Services.AddTransient<ImportDeckPage>();
		builder.Services.AddTransient<DecksPage>();
		builder.Services.AddTransient<DeckSettingsPage>();
		builder.Services.AddTransient<DeckDetailPage>();
		builder.Services.AddTransient<DeckSelectionPage>();
		builder.Services.AddTransient<StepViewerPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<CollectionsPage>();
		builder.Services.AddTransient<BinderSettingsPage>();
		builder.Services.AddTransient<BinderDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}