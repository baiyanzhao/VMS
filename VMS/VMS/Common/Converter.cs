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
	/// 根据Sha,获取版本信息
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

	/// <summary>
	/// 根据Sha,获取当前提交的更改列表
	/// </summary>
	public class CommitDiffConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Global.GetDiff(value as string);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
