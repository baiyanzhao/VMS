using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Shell;
using Serilog;

namespace VMS
{
	/// <summary>
	/// App.xaml 的交互逻辑
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		private const string Unique = "VMS_Unique_Application_String";
		public App()
		{
			Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.File("log/" + DateTime.Now.ToLongDateString() + "/" + DateTime.Now.Ticks + ".log").CreateLogger();
			DispatcherUnhandledException += (s, e) =>
			{
				Log.Fatal(e.Exception, "UnhandledException");
				MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception, "Exception");
				(Current.MainWindow as IDisposable)?.Dispose();
				Environment.Exit(0);
			};

			Exit += delegate
			{
				SingleInstance<App>.Cleanup();
			};

			if(!SingleInstance<App>.InitializeAsFirstInstance(Unique))
			{
				Environment.Exit(0);
			}

			try
			{
				Git.Cmd(null, "lfs version");
			}
			catch
			{
				MessageBox.Show("未正确安装Git\n 请下载安装完整版 Git For Windows", "缺少依赖项!", MessageBoxButton.OK, MessageBoxImage.Error);
				Environment.Exit(0);
			}
		}

		private void ShowMainWindow()
		{
			var window = Current.MainWindow;
			if(window == null)
				return;

			window.Visibility = Visibility.Visible;
			window.WindowState = WindowState.Maximized;
			window.Activate();
		}

		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			ShowMainWindow();
			return true;
		}

		//系统关机拦截
		void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
		{
			e.Cancel = true;
			if(View.ProgressWindow.Worker != null)
				return;

			MainWindow?.Close();
		}
	}
}
