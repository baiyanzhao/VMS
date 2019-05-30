using System.Collections.Generic;
using System.Windows;
using VMS.Model;

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

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static string ShowWindow(Window owner, ICollection<StatusEntryInfo> status, VersionInfo version)
		{
			var commitWindow = new CommitWindow() { Owner = owner };
			commitWindow.Status.DataContext = status;
			commitWindow.Version.DataContext = version;
			if(commitWindow.ShowDialog() != true)
				return null;

			return commitWindow.Message.Text;
		}
	}
}
