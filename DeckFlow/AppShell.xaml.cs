namespace DeckFlow;

using DeckFlow.Views;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(ImportCollectionPage), typeof(ImportCollectionPage));
		Routing.RegisterRoute(nameof(ImportDeckPage), typeof(ImportDeckPage));
		Routing.RegisterRoute(nameof(DecksPage), typeof(DecksPage));
		Routing.RegisterRoute(nameof(DeckSettingsPage), typeof(DeckSettingsPage));
		Routing.RegisterRoute(nameof(DeckDetailPage), typeof(DeckDetailPage));
		Routing.RegisterRoute(nameof(DeckSelectionPage), typeof(DeckSelectionPage));
		Routing.RegisterRoute(nameof(StepViewerPage), typeof(StepViewerPage));
		Routing.RegisterRoute(nameof(CollectionsPage), typeof(CollectionsPage));
		Routing.RegisterRoute(nameof(BinderSettingsPage), typeof(BinderSettingsPage));
		Routing.RegisterRoute(nameof(BinderDetailPage), typeof(BinderDetailPage));
	}
}