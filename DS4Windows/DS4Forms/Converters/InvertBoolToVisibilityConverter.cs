﻿using System.Globalization;
using System.Windows.Data;

namespace DS4WinWPF.DS4Forms.Converters;

public class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool temp = System.Convert.ToBoolean(value);
        temp = !temp;
        Visibility result = Visibility.Visible;
        if (!temp)
        {
            result = Visibility.Hidden;
        }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}