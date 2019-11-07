using System;
using System.Windows;

namespace VMS
{
	/// <summary>
	/// App.xaml 的交互逻辑
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			DispatcherUnhandledException += (s, e) =>
			{
				MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception, "Exception");
				Environment.Exit(0);
			};
		}

		//系统关机拦截
		void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
		{
			e.Cancel = true;
			MainWindow?.Close();
		}
	}
}
