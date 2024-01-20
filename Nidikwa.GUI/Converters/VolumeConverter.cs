using System.Globalization;
using System.Windows.Data;

namespace Nidikwa.GUI.Converters;

[ValueConversion(typeof(float), typeof(string))]
public class VolumeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        float? volume = value as float?;
        if (volume is not null)
        {
            return ((int)(volume * 100)).ToString("000");
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string str = value.ToString() ?? "0";
        if (float.TryParse(str, out float volume))
        {
            return volume / 100;
        }
        return Binding.DoNothing;
    }
}
