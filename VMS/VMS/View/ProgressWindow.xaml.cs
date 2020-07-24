using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace VMS.View
{
	/// <summary>
	/// ProgressWindow.xaml 的交互逻辑
	/// </summary>
	public sealed partial class ProgressWindow : Window
	{
		public static BackgroundWorker Worker { get; private set; } = null;

		public ProgressWindow()
		{
			InitializeComponent();
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e) => e.Handled = true;   //屏蔽所有按键

		public static void Update(string msg)
		{
			if(Worker?.IsBusy == true)
			{
				Worker.ReportProgress(0, msg);
			}
		}

		/// <summary>
		/// 弹出进度条
		/// </summary>
		/// <param name="owner">弹出窗体的Owner</param>
		/// <param name="work">任务方法</param>
		/// <param name="completed">任务完成方法</param>
		/// <returns>是否完成任务</returns>
		public static bool Show(Window owner, Action work, Action completed = null)
		{
			if(Worker != null)
			{
				work?.Invoke();
				return true;
			}

			var isCompleted = true;
			owner = (owner == null && Application.Current.MainWindow.IsLoaded) ? Application.Current.MainWindow : owner;
			var dlg = new ProgressWindow() { Owner = owner };
			if(owner == null)
			{
				dlg.ShowInTaskbar = true;
				dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}
			Worker = new BackgroundWorker() { WorkerReportsProgress = true };

			Worker.DoWork += delegate
			{
				work?.Invoke();
				Thread.Sleep(10);
			};

			Worker.ProgressChanged += (s, e) =>
			{
				dlg.MessageText.Text = e.UserState as string;
			};

			Worker.RunWorkerCompleted += (s, e) =>
			{
				try
				{
					completed?.Invoke();
					if(e.Error != null)
						throw e.Error;
				}
				catch(Exception x)
				{
					if(e.Error != null)
					{
						x = e.Error;
					}

					isCompleted = false;
					Serilog.Log.Error(x, "线程异常!");
					MessageBox.Show(owner, x.Message, "线程异常!", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				finally
				{
					dlg.Close();
				}
			};
			_ = NativeMethods.SetThreadExecutionState(NativeMethods.ExecutionFlag.System | NativeMethods.ExecutionFlag.Continus);
			Worker.RunWorkerAsync();
			dlg.ShowDialog();
			Worker.Dispose();
			Worker = null;
			_ = NativeMethods.SetThreadExecutionState(NativeMethods.ExecutionFlag.Continus);
			return isCompleted;
		}
	}
}
