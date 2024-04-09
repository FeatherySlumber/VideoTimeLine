using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VideoTimeLine
{
    class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            value is TimeSpan ts ? ts.TotalSeconds : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
            TimeSpan.FromSeconds((double)value);
    }
    /*
    class InvertTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            TimeSpan.FromSeconds((double)value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
            value is TimeSpan ts ? ts.TotalSeconds : DependencyProperty.UnsetValue;
    }
    */
    class ValueChangedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 
            value is RoutedPropertyChangedEventArgs<double> rpc ? TimeSpan.FromSeconds(rpc.NewValue) : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
