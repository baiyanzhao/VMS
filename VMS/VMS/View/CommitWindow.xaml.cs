using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VMS.Model;

namespace VMS
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
