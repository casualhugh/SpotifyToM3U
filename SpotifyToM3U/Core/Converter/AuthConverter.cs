using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SpotifyToM3U.Core.Converter
{
    /// <summary>
    /// Converter for authentication button text
    /// </summary>
    public class AuthButtonConverter : IValueConverter
    {
        public static readonly AuthButtonConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAuthenticated)
            {
                return isAuthenticated ? "🚪 Logout" : "🔑 Login to Spotify";
            }
            return "🔑 Login to Spotify";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for track status colors
    /// </summary>
    public class StatusColorConverter : IValueConverter
    {
        public static readonly StatusColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLocal)
            {
                return new SolidColorBrush(isLocal ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
            }
            return new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for track status icons
    /// </summary>
    public class StatusIconConverter : IValueConverter
    {
        public static readonly StatusIconConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLocal)
            {
                return isLocal ? "✓" : "✗";
            }
            return "✗";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that shows visibility when count is zero
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public static readonly ZeroToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for percentage formatting
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public static readonly PercentageConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int found && parameter is int total && total > 0)
            {
                double percentage = (double)found / total * 100;
                return $"{percentage:F0}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for inverting boolean values
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// Converter for error background colors
    /// </summary>
    public class ErrorColorConverter : IValueConverter
    {
        public static readonly ErrorColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasError)
            {
                return new SolidColorBrush(hasError ? Color.FromRgb(244, 67, 54) : Color.FromRgb(76, 175, 80));
            }
            return new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that shows visibility when string is not empty
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}