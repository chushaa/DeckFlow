namespace DeckFlow.Services;

public enum ThemePreference
{
	System,
	Light,
	Dark
}

public class ThemeService
{
	private const string ThemePreferenceKey = "theme_preference";

	public ThemePreference CurrentPreference { get; private set; }

	public ThemeService()
	{
		CurrentPreference = LoadPreference();
		ApplyTheme(CurrentPreference);
	}

	public void SetTheme(ThemePreference preference)
	{
		CurrentPreference = preference;
		Preferences.Set(ThemePreferenceKey, (int)preference);
		ApplyTheme(preference);
	}

	private static ThemePreference LoadPreference()
	{
		var stored = Preferences.Get(ThemePreferenceKey, (int)ThemePreference.System);
		if (Enum.IsDefined(typeof(ThemePreference), stored))
			return (ThemePreference)stored;

		return ThemePreference.System;
	}

	private static void ApplyTheme(ThemePreference preference)
	{
		if (Application.Current is null)
			return;

		Application.Current.UserAppTheme = preference switch
		{
			ThemePreference.Light => AppTheme.Light,
			ThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}
}