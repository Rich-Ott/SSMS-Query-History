using System;
using System.Windows.Data;

namespace SSMSQueryHistory
{
    /// <summary>
    /// Converter used to remove newlines from a string
    /// </summary>
    public class RemoveNewLinesConverter : IValueConverter
    {
        /// <summary>
        /// Removes any newline (\r and \n) characters and replaces them with a single space character
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                value = value.ToString().Replace("\r", " ").Replace("\n", " ");
            }
            return value;
        }

        /// <summary>
        /// Just returns the same value (since I can't replace new lines that no longer exist)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
