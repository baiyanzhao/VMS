using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VMS.View
{
	/// <summary>
	/// WindowSetting.xaml 的交互逻辑
	/// </summary>
	public partial class SettingWindow : Window
	{
		public SettingWindow()
		{
			InitializeComponent();
			Closing += (s, e) =>
			{
				foreach(var item in TopPannel.Children.OfType<TextBox>())
				{
					if(string.IsNullOrWhiteSpace(item.Text))
					{
						e.Cancel = true;
						return;
					}
				}
			};
		}
	}
}
