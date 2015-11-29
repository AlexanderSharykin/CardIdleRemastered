using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered
{
    /// <summary>
    /// Converter provides a frienly name for Enum values from App Resources
    /// <see cref="http://stackoverflow.com/questions/29658721/enum-in-wpf-comboxbox-with-localized-names"/>
    /// <seealso cref="http://stackoverflow.com/a/29659265/1506454"/>
    /// </summary>
    public sealed class EnumLocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)            
                return null;            

            return GetLocalValue((Enum)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value.ToString();            

            foreach (Enum enumValue in Enum.GetValues(targetType))
            {
                if (str == Resources.ResourceManager.GetString(GetResourceKey(targetType, enumValue)))                
                    return enumValue;                
            }

            return value;
        }

        private static string GetResourceKey(Type t, Enum value)
        {
            return String.Format("{0}_{1}", t.Name, value);
        }

        public static string GetLocalValue(Enum value)
        {
            var t = value.GetType();
            var s = Resources.ResourceManager.GetString(GetResourceKey(t, value));
            return s ?? value.ToString();
        }
    }
}
