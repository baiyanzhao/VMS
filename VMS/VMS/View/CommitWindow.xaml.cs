using System.Collections.Generic;
using System.Windows;
using VMS.Model;
using VMS.ViewModel;

namespace VMS.View
{
	/// <summary>
	/// Window1.xaml 的交互逻辑
	/// </summary>
	public partial class CommitWindow : Window
	{
		public CommitWindow() => InitializeComponent();

		private void Commit_Click(object sender, RoutedEventArgs e) => DialogResult = true;
	}
}
