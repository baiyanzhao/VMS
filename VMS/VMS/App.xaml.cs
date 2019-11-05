using System;
using System.Collections.Generic;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Shell;

namespace VMS
{
	/// <summary>
	/// App.xaml 的交互逻辑
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		private TaskbarIcon _taskbar;
		private const string Unique = "VMS_Unique_Application_String";
		public App()
		{
			DispatcherUnhandledException += (s, e) =>
			{
				MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception, "Exception");
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
		}

		private void ShowMainWindow()
		{
			var window = Current.MainWindow;
			if(window == null)
				return;

			if(window.Visibility == Visibility.Hidden)
			{
				window.Visibility = Visibility.Visible;
				window.WindowState = WindowState.Maximized;
				window.Activate();
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			_taskbar = FindResource("Taskbar") as TaskbarIcon;
			_taskbar.LeftClickCommand = new DelegateCommand((parameter) => { ShowMainWindow(); });
			base.OnStartup(e);
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
			MainWindow?.Close();
		}
	}
}
