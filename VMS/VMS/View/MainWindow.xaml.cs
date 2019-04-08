using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Deployment.Application;
using VMS.Data;

namespace VMS
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		static ICollection<TagInfo> _tagInfos = new Collection<TagInfo>(); //Tag信息
		static ICollection<BranchInfo> _branchInfos = new Collection<BranchInfo>(); //分支信息

		public MainWindow()
		{
			InitializeComponent();
		}

		~MainWindow()
		{
			foreach(var item in Directory.GetFiles(Path.GetTempPath(), "*.tmp", SearchOption.TopDirectoryOnly))
			{
				try
				{
					File.SetAttributes(item, FileAttributes.Normal);
					File.Delete(item);
				}
				catch(Exception)
				{ }
			}
		}

		private void Init()
		{
			if(Global.Setting.User == null)
			{
				Dispatcher.Invoke(delegate { ShowSetWindow(); });
			}

			UpdateRepoData();
			Directory.CreateDirectory(Global.Setting.PackageFolder);
		}

		/// <summary>
		/// 更新仓库数据
		/// </summary>
		private void UpdateRepoData()
		{
			try
			{
				//更新仓库
				if(Repository.Discover(Global.Setting.LoaclRepoPath) == null)
				{
					Repository.Clone(Global._preset.RepoUrl, Global.Setting.LoaclRepoPath);
				}

				using(var repo = new Repository(Global.Setting.LoaclRepoPath))
				{
					Commands.Fetch(repo, "origin", new string[0], null, null);

					_tagInfos.Clear();
					foreach(var tag in repo.Tags)
					{
						if(!(tag.Target is Commit commit))
							continue;

						var name = tag.FriendlyName;
						if(!System.Version.TryParse(name, out System.Version version))
							continue;

						_tagInfos.Add(new TagInfo { Version = name, Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
					}

					_branchInfos.Clear();
					foreach(var branch in repo.Branches.Where(p => p.IsRemote))
					{
						var commit = branch.Tip;
						var name = branch.FriendlyName.Split('/').Last();
						if(commit == null || !System.Version.TryParse(name, out System.Version version))
							continue;

						_branchInfos.Add(new BranchInfo { Sha = commit.Sha, Version = name, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
					}
				}
			}
			catch(Exception x)
			{
				Dispatcher.Invoke(delegate { MessageBox.Show(this, x.Message, "同步失败!"); });
			}
		}

		private void UpdateView()
		{
			General.DataContext = null;
			Special.DataContext = null;
			General.DataContext = _tagInfos;
			Special.DataContext = _branchInfos;

			//标题栏
			StringBuilder title = new StringBuilder("源程序版本管理系统 v");
			title.Append(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
			title.Append("    当前分支:");
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				var cmt = repo.Head.Tip as Commit;
				var name = (repo.Head.IsTracking) ? repo.Head.FriendlyName : repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(cmt.Id))?.FriendlyName;   //Head为分支则显示分支名称,否则显示Tag名称
				title.Append(name);
				title.Append(" ");
				title.Append(cmt.Author.Name);
				title.Append(" ");
				title.Append(cmt.Author.When.ToString("yyyy-MM-dd HH:mm"));
			}
			Title = title.ToString();
		}

		ListBoxItem CreateButtonItem(string name, string committishOrBranchSpec)
		{
			var panel = new DockPanel();
			var text = new TextBlock { Text = name };
			var newBranch = new Button { Background = null, Margin = new Thickness(12, 0, 0, 0), BorderThickness = new Thickness(), Content = new Image() { Height = 32, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri("pack://application:,,,/VMS;Component/Images/Add.png")) }, ToolTip = "基于此版本新建" };
			var openBranch = new Button { Background = null, Margin = new Thickness(12, 0, 0, 0), BorderThickness = new Thickness(), Content = new Image() { Height = 32, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri("pack://application:,,,/VMS;Component/Images/Checkout.png")) }, ToolTip = "切换到此版本" };
			var item = new ListBoxItem() { Content = panel, HorizontalContentAlignment = HorizontalAlignment.Stretch };

			newBranch.Click += delegate { Checkout(committishOrBranchSpec); };
			openBranch.Click += delegate { Checkout(committishOrBranchSpec); };

			DockPanel.SetDock(newBranch, Dock.Right);
			DockPanel.SetDock(openBranch, Dock.Right);
			panel.Children.Add(newBranch);
			panel.Children.Add(openBranch);
			panel.Children.Add(text);
			return item;
		}

		void CreateBranch(Repository repo, Tag tag)
		{
			var name = tag.FriendlyName + "-Auto";
			var commit = repo.Lookup<Commit>(tag.Target.Id);
			var bc = repo.Branches.Add(name, commit, true);
			repo.Branches.Update(bc, (s) => { s.TrackedBranch = "refs/remotes/origin/" + name; });
			repo.Network.Push(bc);
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>提交成功, true: 否则,false</returns>
		private bool Commit()
		{
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(!entries.IsDirty || !repo.Head.IsTracking)
					return true;

				List<string> verFiles = new List<string>(); //AssemblyInfo文件的相对路径
				var allProperties = Directory.GetDirectories(Global.Setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories); //版本配置文件所在目录的相对路径
				for(int i = 0; i < allProperties.Length; i++)
				{
					allProperties[i] = Path.GetDirectoryName(allProperties[i]).Substring(Global.Setting.LoaclRepoPath.Length).Replace('\\', '/');
				}

				var infos = new ObservableCollection<StatusEntryInfo>(); //文件信息
				var commitWindow = new CommitWindow() { Owner = this };
				commitWindow.BranchName.Text = repo.Head.FriendlyName;
				foreach(var item in entries)
				{
					switch(item.State)
					{
					case FileStatus.NewInWorkdir:
					case FileStatus.ModifiedInWorkdir:
					case FileStatus.TypeChangeInWorkdir:
					case FileStatus.RenamedInWorkdir:
						repo.Index.Add(item.FilePath);
						infos.Add(new StatusEntryInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
						foreach(var path in allProperties)
						{
							if(item.FilePath.Contains(path))
							{
								var file = path + "/Properties/AssemblyInfo.cs";
								if(!verFiles.Contains(file))
								{
									verFiles.Add(file);
								}
							}
						}
						break;

					case FileStatus.DeletedFromWorkdir:
						repo.Index.Remove(item.FilePath);
						infos.Add(new StatusEntryInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
						break;

					default:
						break;
					}
				}

				commitWindow.Info.DataContext = infos;
				if(commitWindow.ShowDialog() != true)
					return false;

				//版本提交说明
				var cmtText = new StringBuilder();
				foreach(var item in verFiles)
				{
					if(AddVersion(Path.Combine(Global.Setting.LoaclRepoPath, item), out string ver))
					{
						cmtText.Append(item.Split('/')[0]);
						cmtText.Append(' ');
						cmtText.Append(ver);
						cmtText.Append('\n');

						repo.Index.Add(item);
					}
				}
				cmtText.Append(commitWindow.Message.Text);

				//提交
				ProgressWindow.Show(this, delegate
				{
					repo.Index.Write();
					var sign = new Signature(Global.Setting.User, Environment.MachineName, DateTime.Now);
					repo.Commit(cmtText.ToString(), sign, sign);
					repo.Network.Push(repo.Head);
					UpdateRepoData();
				}, UpdateView);
			}
			return true;
		}

		/// <summary>
		/// 上传时,自动升级版本号
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private bool AddVersion(string file, out string newVersion)
		{
			newVersion = string.Empty;
			try
			{
				var lines = File.ReadAllLines(file, Encoding.UTF8);
				const string verKey = "[assembly: AssemblyFileVersion(\"";
				for(int i = 0; i < lines.Length; i++)
				{
					if(lines[i].IndexOf(verKey) == 0)
					{
						var strVersion = lines[i].Substring(verKey.Length).Split(new char[] { '\"' })[0];
						if(System.Version.TryParse(strVersion, out System.Version version))
						{
							newVersion = (new System.Version(version.Major, version.Minor, version.Build, version.Revision + 1)).ToString();
							lines[i] = lines[i].Replace(strVersion, newVersion);
						}
						break;
					}
				}
				File.WriteAllLines(file, lines, Encoding.UTF8);
				return true;
			}
			catch(Exception)
			{ }
			return false;
		}

		/// <summary>
		/// 更新分支信息
		/// </summary>
		private void UpdateBranchInfo()
		{
			TopTab.SelectedIndex = 0;
		}

		private void ShowSetWindow()
		{
			var setWindow = new SettingWindow() { Owner = this };
			setWindow.TopPannel.DataContext = Global.Setting;
			setWindow.ShowDialog();
			Global.Setting.LoaclRepoPath = Global.Setting.LoaclRepoPath.Last() == '\\' ? Global.Setting.LoaclRepoPath : Global.Setting.LoaclRepoPath + "\\";
			File.WriteAllText(Global.FILE_SETTING, new JavaScriptSerializer().Serialize(Global.Setting));
		}

		/// <summary>
		/// 签出版本
		/// </summary>
		/// <param name="committishOrBranchSpec"></param>
		public static void Checkout(string committishOrBranchSpec)
		{
			if(Operate.Checkout(Global.Setting.LoaclRepoPath, committishOrBranchSpec))
			{
				//当前导出项为分支时,配置上游分支
				using(var repo = new Repository(Global.Setting.LoaclRepoPath))
				{
					try
					{
						repo.Branches.Update(repo.Head, (s) => { s.TrackedBranch = "refs/remotes/origin/" + repo.Head.FriendlyName; });
					}
					catch
					{ }
				}

				if(Application.Current.MainWindow is MainWindow window)
				{
					ProgressWindow.Show(window, window.UpdateRepoData, window.UpdateView);
				}
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			ProgressWindow.Show(this, Init, UpdateView);
		}

		private void Open_Click(object sender, RoutedEventArgs e)
		{
			var prj = Directory.GetFiles(Global.Setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories);
			if(prj.Length > 0)
			{
				Process.Start(prj[0]);
			}
		}

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			Commit();
		}

		private void Package_Click(object sender, RoutedEventArgs e)
		{
			ProgressWindow.Show(this, delegate
			{
				foreach(var item in Directory.GetFiles(Global.Setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
						Arguments = "/t:publish /p:Configuration=Release /noconsolelogger \"" + item + "\"",
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden

					}).WaitForExit();
				}

				foreach(var item in Directory.GetFiles(Global.Setting.LoaclRepoPath, "setup.exe", SearchOption.AllDirectories))
				{
					var dir = Path.GetDirectoryName(item);
					var app = Directory.GetFiles(dir, "*.application");
					if(app.Length <= 0)
						continue;

					var rarPath = Path.Combine(Environment.CurrentDirectory, "Package\\");
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine(rarPath, "WinRAR.exe"),
						Arguments = string.Format("a -r -s -sfx -z{0} -iicon{1} -iadm -ibck \"{2}\"", rarPath + "sfx", rarPath + "msi.ico", Path.Combine(Global.Setting.PackageFolder, Path.GetFileNameWithoutExtension(app[0]))),
						WorkingDirectory = dir,
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}).WaitForExit();
				}
				Process.Start(Global.Setting.PackageFolder);
			});
		}

		private void Set_Click(object sender, RoutedEventArgs e)
		{
			ShowSetWindow();
		}
	}
}
