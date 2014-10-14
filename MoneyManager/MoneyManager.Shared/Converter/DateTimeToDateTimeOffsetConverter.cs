using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace MoneyManager.Converter
{
    public class DateTimeToDateTimeOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                var date = (DateTime)value;
                return new DateTimeOffset(date).ToString("d", CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                return DateTimeOffset.MinValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            try
            {
                var dto = (DateTimeOffset)value;
                return dto.DateTime.ToString("d", CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
    }
}