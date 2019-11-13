using System;
using System.Globalization;
using System.Windows.Data;

namespace VMS
{
	/// <summary>
	/// 判断字符串空白
	/// </summary>
	public class EmptyStringConvert : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !string.IsNullOrWhiteSpace(value as string);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// 转化版本号
	/// </summary>
	class VersionConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value switch
			{
				Version version => version.ToString(2),
				_ => value,
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}

	/// <summary>
	/// 字符串匹配
	/// </summary>
	public class StringMatchConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if(values != null && values.Length >= 2 && values[0] is string full && values[1] is string head)
			{
				return full.StartsWith(head);
			}
			return false;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	/// <summary>
	/// 字符串匹配
	/// </summary>
	public class BranchDetailConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Global.ReadVersionInfo(value as string);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
