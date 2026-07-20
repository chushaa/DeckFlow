namespace DeckFlow.Converters;

using System.Globalization;

public class StringToColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string hex && Color.TryParse(hex, out var color))
			return color;

		return Colors.Gray;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}