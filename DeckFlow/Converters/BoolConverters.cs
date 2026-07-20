namespace DeckFlow.Converters;

using System.Globalization;

public class InvertedBoolConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b && !b;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b && !b;
}

public class IsNotNullConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is not null;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}

public class IsNotNullOrEmptyConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is string s && !string.IsNullOrEmpty(s);

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}