
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CardIdleRemastered.Converters
{
    public class BooleanToLengthConverter : IValueConverter
    {
        private GridLengthConverter lengthConverter = new GridLengthConverter();
        
        #region IValueConverter implementation

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value is bool) && (bool)value && parameter != null)
                return lengthConverter.ConvertFromInvariantString(parameter.ToString());
            return new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
