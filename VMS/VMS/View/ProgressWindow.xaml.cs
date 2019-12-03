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
		static BackgroundWorker sInit = null;

		public ProgressWindow()
		{
			InitializeComponent();
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;   //屏蔽所有按键
		}

		public static void Update(string msg)
		{
			sInit?.ReportProgress(0, msg);
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
			bool isCompleted = true;
			var dlg = new ProgressWindow() { Owner = owner };
			sInit = new BackgroundWorker() { WorkerReportsProgress = true };

			sInit.DoWork += delegate
			{
				work?.Invoke();
				Thread.Sleep(10);
			};

			sInit.ProgressChanged += (s, e) =>
			{
				dlg.MessageText.Text = e.UserState as string;
			};

			sInit.RunWorkerCompleted += (s, e) =>
			{
				try
				{
					completed?.Invoke();
					if(e.Error != null)
					{
						throw e.Error;
					}
				}
				catch(Exception x)
				{
					isCompleted = false;
					MessageBox.Show(x.Message + "\n" + x.StackTrace, "后台线程异常!", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				finally
				{
					dlg.Close();
				}
			};
			sInit.RunWorkerAsync();
			dlg.ShowDialog();
			sInit.Dispose();
			sInit = null;
			return isCompleted;
		}
	}
}
