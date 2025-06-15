using System;
using System.Globalization;
using System.Windows.Data;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Converter that transforms a progress value (0-1) to a width value for progress bars
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        /// <summary>
        /// Singleton instance for easy access
        /// </summary>
        public static readonly ProgressToWidthConverter Instance = new();

        /// <summary>
        /// Converts progress value and maximum width to actual width
        /// </summary>
        /// <param name="values">Array containing [progress (float), maxWidth (double)]</param>
        /// <param name="targetType">Target type (should be double)</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture info (not used)</param>
        /// <returns>Calculated width as double</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate input
            if (values == null || values.Length != 2)
                return 0.0;

            try
            {
                // Extract progress value (0.0 to 1.0)
                double progress = 0.0;
                if (values[0] is float progressFloat)
                    progress = progressFloat;
                else if (values[0] is double progressDouble)
                    progress = progressDouble;
                else if (values[0] is int progressInt)
                    progress = progressInt / 100.0; // Assume percentage
                else
                    return 0.0;

                // Extract maximum width
                double maxWidth = 0.0;
                if (values[1] is double maxWidthDouble)
                    maxWidth = maxWidthDouble;
                else if (values[1] is float maxWidthFloat)
                    maxWidth = maxWidthFloat;
                else if (values[1] is int maxWidthInt)
                    maxWidth = maxWidthInt;
                else if (double.TryParse(values[1]?.ToString(), out double parsedWidth))
                    maxWidth = parsedWidth;
                else
                    return 0.0;

                // Clamp progress to valid range
                progress = Math.Max(0.0, Math.Min(1.0, progress));

                // Calculate and return width
                double resultWidth = progress * maxWidth;
                return Math.Max(0.0, resultWidth);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProgressToWidthConverter: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Converts back (not implemented as it's not needed for progress bars)
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not supported for ProgressToWidthConverter");
        }
    }

    /// <summary>
    /// Simple single-value converter for basic progress scenarios
    /// </summary>
    public class SimpleProgressToWidthConverter : IValueConverter
    {
        public static readonly SimpleProgressToWidthConverter Instance = new();

        public double MaxWidth { get; set; } = 100.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double progress = 0.0;

                if (value is float progressFloat)
                    progress = progressFloat;
                else if (value is double progressDouble)
                    progress = progressDouble;
                else if (double.TryParse(value?.ToString(), out double parsedProgress))
                    progress = parsedProgress;
                else
                    return 0.0;

                // Get max width from parameter if provided
                double maxWidth = MaxWidth;
                if (parameter != null && double.TryParse(parameter.ToString(), out double paramWidth))
                    maxWidth = paramWidth;

                // Clamp and calculate
                progress = Math.Max(0.0, Math.Min(1.0, progress));
                return Math.Max(0.0, progress * maxWidth);
            }
            catch
            {
                return 0.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}