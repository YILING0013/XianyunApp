using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace xianyun.Common
{
    public class NullableLongConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as long?)?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert from string to long?
            if (string.IsNullOrWhiteSpace(value as string))
            {
                return null;
            }

            if (long.TryParse(value as string, out long result))
            {
                return result;
            }

            throw new FormatException("Invalid input for long");
        }
    }

}
