using System;
using System.Globalization;
using System.Windows.Data;

namespace SpotifyToM3U.MVVM.View
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public static readonly ProgressToWidthConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float progress && parameter is double maxWidth)
            {
                return Math.Max(0, progress * maxWidth);
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}