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

		public static void Show(Window owner, System.Action work, System.Action completed = null)
		{
			ProgressWindow dlg = new ProgressWindow() { Owner = owner };
			sInit = new BackgroundWorker() { WorkerReportsProgress = true };

			sInit.DoWork += delegate
			{
				try
				{
					work?.Invoke();
					Thread.Sleep(100);
				}
				catch(System.Exception x)
				{
					dlg.Dispatcher.Invoke(delegate { MessageBox.Show(dlg, x.StackTrace, x.Message); });
				}
			};

			sInit.RunWorkerCompleted += delegate
			{
				try
				{
					completed?.Invoke();
				}
				catch(System.Exception x)
				{
					dlg.Dispatcher.Invoke(delegate { MessageBox.Show(dlg, x.StackTrace, x.Message); });
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
