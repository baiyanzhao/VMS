using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Data;
using LibGit2Sharp;
using VMS.Model;
using VMS.ViewModel;

namespace VMS.View
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly BranchInfoView _branchInfos = new BranchInfoView(); //分支信息

		public MainWindow()
		{
			InitializeComponent();
			StateChanged += delegate
			{
				if(WindowState == WindowState.Minimized)
				{
					Hide();
				}
			};

			if(Global.Setting.User == null)
			{
				ShowSetWindow();
			}

			Directory.CreateDirectory(Global.Setting.PackageFolder);
			ProgressWindow.Show(null, Global.Git.Sync, UpdateBranchInfo);
		}

		~MainWindow()
		{
			//清理临时文件
			foreach(var item in Directory.GetFiles(Path.GetTempPath(), "*.tmp", SearchOption.TopDirectoryOnly))
			{
				try
				{
					File.SetAttributes(item, FileAttributes.Normal);
					File.Delete(item);
				}
				catch
				{ }
			}
		}

		/// <summary>
		/// 更新界面
		/// </summary>
		private void UpdateBranchInfo()
		{
			using var repo = new Repository(Global.Setting.LoaclRepoPath);
			//更新分支列表
			_branchInfos.Clear();
			foreach(var tag in repo.Tags)
			{
				if(!(tag.Target is Commit commit))
					continue;

				var name = tag.FriendlyName;
				if(!System.Version.TryParse(name, out var version))
					continue;

				_branchInfos.Add(new BranchInfo { Type = GitType.Tag, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
			}

			foreach(var branch in repo.Branches.Where(p => p.IsRemote))
			{
				var commit = branch.Tip;
				var name = branch.FriendlyName.Split('/').Last();
				if(commit == null || !System.Version.TryParse(name, out var version))
					continue;

				_branchInfos.Add(new BranchInfo { Type = GitType.Branch, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
			}

			_branchInfos.HeadName = (repo.Head.IsTracking) ? repo.Head.FriendlyName : repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(repo.Head.Tip.Id))?.FriendlyName;   //Head为分支则显示分支名称,否则显示Tag名称
			Application.Current.MainWindow.Title = "版本管理 分支:" + _branchInfos.HeadName + " " + repo.Head.Tip.Author.Name;
			BranchInfoGrid.DataContext = _branchInfos;  //在界面显示前,设定上下文
		}

		/// <summary>
		/// 显示设置界面
		/// </summary>
		private void ShowSetWindow()
		{
			var window = new SettingWindow() { Owner = IsLoaded ? this : null };
			window.TopPannel.DataContext = Global.Setting;
			window.ShowDialog();
			Global.Setting.PackageFolder = Global.Setting.PackageFolder.Last() == '\\' ? Global.Setting.PackageFolder : Global.Setting.PackageFolder + "\\";
			Global.Setting.LoaclRepoPath = Global.Setting.LoaclRepoPath.Last() == '\\' ? Global.Setting.LoaclRepoPath : Global.Setting.LoaclRepoPath + "\\";
			File.WriteAllText(Global.FILE_SETTING, new JavaScriptSerializer().Serialize(Global.Setting));
		}

		public static void ShowLogWindow(string name, System.Version version, string sha)
		{
			var infos = new BranchInfoView();
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				var commit = repo.Lookup<Commit>(sha);
				if(commit == null)
					return;

				while(true)
				{
					infos.Add(new BranchInfo { Name = name, Version = version, Type = GitType.Sha, Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
					commit = commit.Parents.FirstOrDefault();
					if(commit == null)
						break;
				}
			}

			var window = new LogWindow() { Owner = Application.Current.MainWindow };
			window.Title = name;
			window.DataContext = infos;
			window.ShowDialog();
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>工程未更改, null; 提交成功, true: 否则,false</returns>
		public static bool? Commit(Repository repo)
		{
			var entries = repo.RetrieveStatus();
			if(!entries.IsDirty)
				return null;

			#region 打开提交对话框
			//读取文件状态
			var assemblyList = Global.GetAssemblyInfo();
			var status = new Collection<StatusEntryInfo>();
			foreach(var item in entries)
			{
				switch(item.State)
				{
				case FileStatus.NewInWorkdir:
				case FileStatus.ModifiedInWorkdir:
				case FileStatus.TypeChangeInWorkdir:
				case FileStatus.RenamedInWorkdir:
				case FileStatus.DeletedFromWorkdir:
					status.Add(new StatusEntryInfo() { FilePath = item.FilePath, FileStatus = item.State });
					foreach(var assembly in assemblyList)
					{
						if(item.FilePath.Contains(assembly.ProjectPath))
						{
							assembly.IsModified = true;
						}
					}
					break;
				default:
					break;
				}
			}

			//填写提交信息
			var versionInfo = Global.ReadVersionInfo()?? new VersionInfo();
			versionInfo.KeyWords ??= new ObservableCollection<VersionInfo.StringProperty>();

			MainWindow instance = null;
			foreach(Window item in Application.Current.Windows)
			{
				if(item.IsActive)
				{
					instance = item as MainWindow;
					break;
				}
			}

			var commitText = CommitWindow.ShowWindow(instance, status, versionInfo);
			if(commitText == null)
				return false;
			#endregion

			#region 先同步上游分支
			if(!repo.Head.IsTracking)
			{
				if(MessageBox.Show("当前为只读版本,是否撤销全部更改?", repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(repo.Head.Tip.Id))?.FriendlyName + " 提交失败", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					repo.Reset(ResetMode.Hard);
					return true;
				}
				return false;
			}

			//以Sys名称拉取上游分支
			repo.Network.Fetch(repo.Network.Remotes.First());
			if(repo.Head.TrackingDetails.BehindBy > 0)
			{
				repo.Network.Pull(new Signature("Sys", Environment.MachineName, DateTime.Now), new PullOptions());
			}

			repo.Network.Fetch(repo.Network.Remotes.First(), new string[] { repo.Head.CanonicalName + ":" + repo.Head.CanonicalName });
			#endregion

			#region 更新版本信息
			System.Version.TryParse(repo.Head.FriendlyName, out var branchVersion);
			versionInfo.VersionNow = versionInfo.VersionNow == null ? branchVersion ?? new System.Version(1, 0, 0, 0) : new System.Version(versionInfo.VersionNow.Major, versionInfo.VersionNow.Minor, versionInfo.VersionNow.Build, versionInfo.VersionNow.Revision + 1);

			versionInfo.VersionList = new List<VersionInfo.StringPair>();
			foreach(var assembly in assemblyList)
			{
				assembly.HitVersion(branchVersion == null ? 0 : branchVersion.Build);
				versionInfo.VersionList.Add(new VersionInfo.StringPair() { Label = Path.GetFileName(assembly.ProjectPath), Value = assembly.Version.ToString() });
			}
			Global.WriteVersionInfo(versionInfo);
			#endregion

			#region 提交
			bool err = false;
			ProgressWindow.Show(instance, delegate
			{
				try
				{
					Global.Git.Commit(repo, versionInfo.VersionNow.ToString() + " " + commitText, (msg) => { ProgressWindow.Update(msg); });
				}
				catch(Exception x)
				{
					err = true;
					instance.Dispatcher.Invoke(delegate
					{
						MessageBox.Show(instance, x.Message, "连接服务器失败,请检查网络连接或重启软件后重试!", MessageBoxButton.OK, MessageBoxImage.Error);
					});
				}
			});

			if(err)
				return false;

			var commit = repo.Head.Tip;
			var name = repo.Head.FriendlyName;
			var info = instance._branchInfos.FirstOrDefault(p => p.Name.Equals(name));
			if(info == null)
			{
				if(!System.Version.TryParse(name, out var version))
					return true; //如果上传非版本分支,如master, 界面不更新

				info = new BranchInfo { Type = GitType.Branch, Name = name, Version = version, Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort };
				instance._branchInfos.Add(info);
			}
			else
			{
				info.Sha = commit.Sha;
				info.Author = commit.Author.Name;
				info.When = commit.Author.When;
				info.Message = commit.MessageShort;
			}
			#endregion

			return true;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//分组和排序显示, GetDefaultView概率为null,改为直接操作Items
			BranchInfoGrid.UpdateLayout();  //分组显示前,先更新布局,防止分组折叠后,部分列显示不完整.
			BranchInfoGrid.Items.GroupDescriptions.Add(new PropertyGroupDescription("Version", new VersionConverter()));
			BranchInfoGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Version", System.ComponentModel.ListSortDirection.Ascending));
		}

		private void Open_Click(object sender, RoutedEventArgs e)
		{
			var prj = Directory.GetFiles(Global.Setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories);
			if(prj.Length > 0)
			{
				Process.Start(prj[0]);
			}
		}

		private void Explorer_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(Global.Setting.LoaclRepoPath);
		}

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			using var repo = new Repository(Global.Setting.LoaclRepoPath);
			if(Commit(repo) == null)
			{
				MessageBox.Show("当前版本无任何更改!", repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(repo.Head.Tip.Id))?.FriendlyName ?? repo.Head.FriendlyName);
			}
		}

		/// <summary>
		/// 生成安装包
		/// </summary>
		private void Package_Click(object sender, RoutedEventArgs e)
		{
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				if(Commit(repo) == false)
					return;
			}

			ProgressWindow.Show(this, delegate
			{
				var folder = Path.Combine(Global.Setting.PackageFolder, Global.ReadVersionInfo()?.VersionNow?.ToString() + "\\");
				Directory.CreateDirectory(folder);

				//生成解决方案
				foreach(var item in Directory.GetFiles(Global.Setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = Global.Setting.MSBuildPath,
						Arguments = "/t:publish /p:Configuration=Release /noconsolelogger \"" + item + "\"",
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}).WaitForExit();
				}

				//生成自解压安装包
				foreach(var item in Directory.GetFiles(Global.Setting.LoaclRepoPath, "setup.exe", SearchOption.AllDirectories))
				{
					var dir = Path.GetDirectoryName(item);
					var app = Directory.GetFiles(dir, "*.application").FirstOrDefault();
					if(string.IsNullOrEmpty(app))
						continue;

					var rarPath = Path.Combine(Environment.CurrentDirectory, "Package\\");
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine(rarPath, "WinRAR.exe"),
						Arguments = string.Format("a -r -s -sfx -z{0} -iicon{1} -iadm -ibck \"{2}\"", rarPath + "sfx", rarPath + "msi.ico", Path.Combine(folder, Path.GetFileNameWithoutExtension(app))),
						WorkingDirectory = dir,
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}).WaitForExit();
				}

				//复制hex文件
				foreach(var item in Directory.GetFiles(Global.Setting.LoaclRepoPath, "*.hex", SearchOption.AllDirectories))
				{
					File.Copy(item, Path.Combine(folder, Path.GetFileName(item)), true);
				}

				//复制bin文件
				foreach(var item in Directory.GetFiles(Global.Setting.LoaclRepoPath, "*.bin", SearchOption.AllDirectories))
				{
					File.Copy(item, Path.Combine(folder, Path.GetFileName(item)), true);
				}
				Process.Start(Global.Setting.PackageFolder);
			});
		}

		private void Set_Click(object sender, RoutedEventArgs e)
		{
			ShowSetWindow();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			using var repo = new Repository(Global.Setting.LoaclRepoPath);
			if(repo.RetrieveStatus().IsDirty)
			{
				switch(MessageBox.Show(Application.Current.MainWindow, "当前版本中存在尚未提交的文件,是否立即提交?\n 点'是', 提交更改\n 点'否', 直接退出\n 点'取消', 不进行任何操作.", "尚有文件未提交", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
				{
				case MessageBoxResult.Yes:
					Commit(repo);
					break;
				case MessageBoxResult.Cancel:
					e.Cancel = true;
					break;
				default:
					break;
				}
			}
		}
	}
}
