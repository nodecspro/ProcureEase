#region

using System.Globalization;
using System.Windows.Data;

#endregion

namespace ProcureEase.Classes;

public class PhoneMaskConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string phone) return FormatPhoneNumber(phone);

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string formattedPhone) return RemovePhoneNumberFormatting(formattedPhone);

        return value;
    }

    private string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 11)
        {
            return $"+{digits[0]} ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
        }

        return phone;
    }

    private string RemovePhoneNumberFormatting(string formattedPhone)
    {
        return new string(formattedPhone.Where(char.IsDigit).ToArray());
    }
}