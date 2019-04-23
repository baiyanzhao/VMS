using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace VMS
{
	/// <summary>
	/// ProgressWindow.xaml 的交互逻辑
	/// </summary>
	public sealed partial class ProgressWindow : Window
	{
		static BackgroundWorker sInit = null;

		public ProgressWindow()
		{
			InitializeComponent();
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;	//屏蔽所有按键
		}

		public static void Show(Window owner, Action work, Action completed = null)
		{
			ProgressWindow dlg = new ProgressWindow() { Owner = owner };
			sInit = new BackgroundWorker() { WorkerReportsProgress = true };

			sInit.DoWork += delegate
			{
				try
				{
					work?.Invoke();
					Thread.Sleep(10);
				}
				catch(Exception x)
				{
					dlg.Dispatcher.Invoke(delegate { MessageBox.Show(dlg, x.Message + "\n" + x.StackTrace, "后台线程异常!"); });
				}
			};

			sInit.RunWorkerCompleted += delegate
			{
				try
				{
					completed?.Invoke();
				}
				catch(Exception x)
				{
					MessageBox.Show(owner, x.StackTrace, x.Message);
				}
				finally
				{
					dlg.Close();
				}
			};
			sInit.RunWorkerAsync();
			dlg.ShowDialog();
			sInit.Dispose();
		}
	}
}
