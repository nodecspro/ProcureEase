#region

using System.Globalization;
using System.IO;
using System.Windows.Data;

#endregion

namespace ProcureEase.Classes;

public class PathToFilenameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = value as string;
        return Path.GetFileName(path);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}