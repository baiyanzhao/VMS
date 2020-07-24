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
				Height = 540,
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

			var btn = new Button { Content = "确定", VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(2, 2, 120, 2), Padding = new Thickness(12, 0, 12, 0) };
			btn.Click += delegate
			{
				window.DialogResult = true;
				if(listBox.SelectedValue == null)
					return;

				Message.Text = listBox.SelectedValue.ToString();
			};

			Grid.SetRow(btn, 1);
			window.Panel.Children.Add(btn);
			window.InputGrid.Children.Add(listBox);
			window.ShowDialog();
		}

		private void DataGrid_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e) => e.Row.Header = string.Format("{0,4} ", e.Row.GetIndex() + 1);
	}
}
