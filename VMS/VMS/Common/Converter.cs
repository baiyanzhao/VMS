using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VMS
{
	/// <summary>
	/// 判断字符串空白
	/// </summary>
	public class EmptyStringConvert : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !string.IsNullOrWhiteSpace(value as string);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}

	/// <summary>
	/// 转化次版本号
	/// </summary>
	internal class VersionMinorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
		{
			Version version => version.ToString(2),
			_ => DependencyProperty.UnsetValue,
		};

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
	}

	/// <summary>
	/// 根据Sha,获取版本信息
	/// </summary>
	public class BranchDetailConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => GlobalShared.ReadVersionInfo(value as string);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
	}

	/// <summary>
	/// 根据Sha,获取当前提交的更改列表
	/// </summary>
	public class CommitDiffConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => GlobalShared.GetDiffList(value as string);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
	}
}
