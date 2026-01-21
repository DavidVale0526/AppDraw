using System.Globalization;

namespace WebViewApp;

public class SelectedColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return Color.FromArgb("#ffffff"); // White for selected
        
        return Color.FromArgb("#e0e0e0"); // Grey for unselected
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
