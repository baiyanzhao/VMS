//using System.Deployment.Application;
using System.Diagnostics;
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
			Title = "源程序版本管理软件 v" + /*ApplicationDeployment.IsNetworkDeployed ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() :*/ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
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

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			//Process.Start(ApplicationDeployment.IsNetworkDeployed ? ApplicationDeployment.CurrentDeployment.UpdateLocation.OriginalString : System.Reflection.Assembly.GetExecutingAssembly().Location);
			System.Environment.Exit(0);
		}
	}
}
