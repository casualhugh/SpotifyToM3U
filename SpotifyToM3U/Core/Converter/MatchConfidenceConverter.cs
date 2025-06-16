using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SpotifyToM3U.Core.Converter
{
    /// <summary>
    /// Converts match confidence (0.0-1.0) to appropriate background color
    /// </summary>
    public class MatchConfidenceColorConverter : IValueConverter
    {
        public static readonly MatchConfidenceColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double confidence)
            {
                return confidence switch
                {
                    >= 0.95 => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green - Perfect match
                    >= 0.85 => new SolidColorBrush(Color.FromRgb(139, 195, 74)),   // Light Green - Very good
                    >= 0.75 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),    // Amber - Good
                    >= 0.65 => new SolidColorBrush(Color.FromRgb(255, 152, 0)),    // Orange - Weak
                    _ => new SolidColorBrush(Color.FromRgb(244, 67, 54))           // Red - Very weak
                };
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray - No confidence data
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts match confidence to appropriate icon/text
    /// </summary>
    public class MatchConfidenceIconConverter : IValueConverter
    {
        public static readonly MatchConfidenceIconConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLocal)
            {
                return isLocal ? "✓ Available" : "✗ Missing";
            }

            return "? Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts match confidence to percentage text
    /// </summary>
    public class MatchConfidenceTextConverter : IValueConverter
    {
        public static readonly MatchConfidenceTextConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double confidence)
            {
                int percentage = (int)(confidence * 100);
                return $"{percentage}%";
            }

            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an object to visibility (visible if not null)
    /// </summary>
    public class ObjectToVisibilityConverter : IValueConverter
    {
        public static readonly ObjectToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts count to visibility (visible if count > 0)
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public static readonly CountToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts match type to user-friendly description
    /// </summary>
    public class MatchTypeDescriptionConverter : IValueConverter
    {
        public static readonly MatchTypeDescriptionConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string matchType)
            {
                return matchType switch
                {
                    "Exact Match" => "🎯 Perfect match found",
                    "High Confidence" => "✅ Very likely match",
                    "Album Confirmed" => "💿 Confirmed by album",
                    "Filename Match" => "📁 Matched by filename",
                    "Fuzzy Match" => "🔍 Similar track found",
                    _ => "❓ Unknown match type"
                };
            }

            return "❓ No match information";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to success/error brush
    /// </summary>
    public class BooleanToSuccessColorConverter : IValueConverter
    {
        public static readonly BooleanToSuccessColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool success)
            {
                return success
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Success green
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Error red
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Neutral gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts two values to a percentage calculation
    /// </summary>
    public class PercentageCalculatorConverter : IMultiValueConverter
    {
        public static readonly PercentageCalculatorConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is int numerator &&
                values[1] is int denominator &&
                denominator > 0)
            {
                double percentage = (double)numerator / denominator * 100;
                return $"{percentage:F0}%";
            }

            return "0%";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts track count and found count to missing count
    /// </summary>
    public class MissingCountConverter : IMultiValueConverter
    {
        public static readonly MissingCountConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is int total &&
                values[1] is int found)
            {
                return Math.Max(0, total - found);
            }

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts confidence value to tooltip text with detailed breakdown
    /// </summary>
    public class MatchConfidenceTooltipConverter : IValueConverter
    {
        public static readonly MatchConfidenceTooltipConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double confidence)
            {
                int percentage = (int)(confidence * 100);
                string description = confidence switch
                {
                    >= 0.95 => "Perfect match - identical title and artist",
                    >= 0.85 => "Very high confidence - minor differences only",
                    >= 0.75 => "Good match - likely correct with small variations",
                    >= 0.65 => "Weak match - may need manual verification",
                    _ => "Very weak match - likely incorrect"
                };

                return $"Match Confidence: {percentage}%\n{description}";
            }

            return "No match confidence data available";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}