using System.Windows;

namespace VMS.View
{
	/// <summary>
	/// BranchWindow.xaml 的交互逻辑
	/// </summary>
	public partial class LogWindow : Window
	{
		public LogWindow()
		{
			InitializeComponent();
		}

		private void DataGrid_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e) => e.Row.Header = string.Format("{0,4} ",e.Row.GetIndex() + 1);
	}
}
