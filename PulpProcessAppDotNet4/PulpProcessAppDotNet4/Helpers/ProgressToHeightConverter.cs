using System;
using System.Globalization;
using System.Windows.Data;

namespace PulpProcessAppDotNet4.Helpers
{
    /// <summary>
    /// Handles the graphics' dynamic lengthening and shortening of the progress bars.
    /// </summary>
    public class ProgressToHeightConverter : IValueConverter
    {
        /// <summary>
        /// Responsible for adjusting the graphics.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // Check if the parameter is "TI300"
                if (parameter is string paramString && paramString == "TI300")
                {
                    return progress * 5; // Use a different multiplier for TI300
                }

                // Default multiplier for other values
                return progress * 0.5; 
            }
            return 0;
        }
        /// <summary>
        /// Satisfies the <see cref="IValueConverter"/> interface.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
