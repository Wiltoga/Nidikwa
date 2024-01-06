using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Windows.Data;

namespace Nidikwa.GUI;

internal class DateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset date)
        {
            return date.ToString("g", CultureInfo.CurrentUICulture);
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
