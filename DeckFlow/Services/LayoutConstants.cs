namespace DeckFlow.Services;

public static class LayoutConstants
{
	public const double CompactMaxWidth = 600;
	public const double MediumMaxWidth = 1000;

	public static LayoutBreakpoint GetBreakpoint(double width)
	{
		if (width < CompactMaxWidth)
			return LayoutBreakpoint.Compact;
		if (width < MediumMaxWidth)
			return LayoutBreakpoint.Medium;

		return LayoutBreakpoint.Expanded;
	}
}

public enum LayoutBreakpoint
{
	Compact,
	Medium,
	Expanded
}