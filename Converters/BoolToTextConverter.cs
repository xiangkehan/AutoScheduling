using System;
using Microsoft.UI.Xaml.Data;

namespace AutoScheduling3.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not bool boolValue)
                return string.Empty;

            string[] texts = parameter as string[] ?? ((string)parameter)?.Split('|');
            if (texts == null || texts.Length < 2)
            {
                return boolValue.ToString();
            }

            return boolValue ? texts[0] : texts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
