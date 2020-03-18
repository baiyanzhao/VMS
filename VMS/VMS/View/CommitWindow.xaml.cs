using System.Windows;
using System.Windows.Controls;

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
			if(!GlobalShared.Settings.LatestMessage.Contains(Message.Text))
			{
				GlobalShared.Settings.LatestMessage.Insert(0, Message.Text);
				if(GlobalShared.Settings.LatestMessage.Count > 10)
				{
					GlobalShared.Settings.LatestMessage.RemoveAt(GlobalShared.Settings.LatestMessage.Count - 1);
				}
			}
			GlobalShared.WriteSetting();
			DialogResult = true;
		}

		private void LatestMessage_Click(object sender, RoutedEventArgs e)
		{
			var window = new InputWindow
			{
				Height = 500,
				Width = 800,
				ShowInTaskbar = false,
				Title = "历史记录",
				Owner = this
			};

			var listBox = new ListBox { Height = 450, Width = 770, VerticalAlignment = VerticalAlignment.Center, Background = null };
			listBox.ItemsSource = GlobalShared.Settings.LatestMessage;
			listBox.MouseDoubleClick += delegate
			{
				window.DialogResult = true;
				Message.Text = listBox.SelectedValue?.ToString();
			};
			window.InputGrid.Children.Add(listBox);
			window.ShowDialog();
		}
	}
}
