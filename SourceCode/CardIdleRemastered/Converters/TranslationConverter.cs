using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered.Converters
{
    public class TranslationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return Resources.ResourceManager.GetString(value.ToString()) ?? value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
