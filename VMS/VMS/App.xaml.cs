using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
	}
}
