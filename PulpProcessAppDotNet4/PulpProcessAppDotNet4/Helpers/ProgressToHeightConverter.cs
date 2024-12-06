using System;
using System.Globalization;
using System.Windows.Data;

namespace PulpProcessAppDotNet4.Helpers
{
    public class ProgressToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // Scale progress to height (assuming max is 100)
                return progress * 0.5; // Adjust multiplier as per actual height scaling
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
