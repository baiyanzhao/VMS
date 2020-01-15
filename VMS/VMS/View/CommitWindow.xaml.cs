using System.Windows;

namespace VMS.View
{
	/// <summary>
	/// Window1.xaml 的交互逻辑
	/// </summary>
	public partial class CommitWindow : Window
	{
		public CommitWindow()
		{
			InitializeComponent();
		}

		private void Commit_Click(object sender, RoutedEventArgs e) => DialogResult = true;
	}
}
