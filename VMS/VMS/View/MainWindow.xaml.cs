using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Hardcodet.Wpf.TaskbarNotification;
using LibGit2Sharp;
using VMS.Model;
using VMS.ViewModel;
using static VMS.GlobalShared;

namespace VMS.View
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public sealed partial class MainWindow : Window, IDisposable
	{
		private readonly TaskbarIcon _taskbar = new TaskbarIcon { Visibility = Visibility.Hidden }; //通知区图标
		private readonly ObservableCollection<BranchInfo> _branchInfos = new ObservableCollection<BranchInfo>(); //分支信息

		#region 公共方法
		public MainWindow()
		{
			InitializeComponent();

			_taskbar.IconSource = Icon;
			_taskbar.LeftClickCommand = TaskbarCmd();
			_taskbar.DoubleClickCommand = TaskbarCmd();

			StateChanged += delegate
			{
				switch(WindowState)
				{
				case WindowState.Normal:
					break;
				case WindowState.Minimized:
					Hide();
					_taskbar.ToolTipText = Title;
					_taskbar.Visibility = Visibility.Visible;
					_taskbar.ShowBalloonTip("程序将在后台运行", Title, BalloonIcon.None);
					break;
				case WindowState.Maximized:
					_taskbar.Visibility = Visibility.Hidden;
					break;
				default:
					break;
				}
			};

			if(Settings.User == null)
			{
				ShowSetWindow();
			}

			Directory.CreateDirectory(Settings.PackageFolder);
			ProgressWindow.Show(null, Git.Sync, UpdateBranchInfo);
		}

		~MainWindow()
		{
			//清理临时文件
			foreach(var item in Directory.GetFiles(Path.GetTempPath(), "*.tmp", SearchOption.TopDirectoryOnly))
			{
				try
				{
					File.Delete(item);
				}
				catch
				{ }
			}
		}

		public void Dispose()
		{
			_taskbar.Dispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 显示历史记录界面
		/// </summary>
		/// <param name="name">名称</param>
		/// <param name="version">版本</param>
		/// <param name="sha">Git提交标识</param>
		public static void ShowLogWindow(string name, System.Version version, string sha)
		{
			var infos = new Collection<BranchInfo>();
			using(var repo = new Repository(Settings.LoaclRepoPath))
			{
				LookupCommit(name, version, repo.Lookup<Commit>(sha), infos);
			}

			new LogWindow { Owner = Application.Current.MainWindow, Title = name, DataContext = infos.OrderByDescending(info => info.When) }.ShowDialog();
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>工程未更改, null; 提交成功, true: 否则,false</returns>
		public static bool? Commit(Repository repo)
		{
			if(repo == null)
				return null;

			var entries = repo.RetrieveStatus();
			if(!entries.IsDirty)
				return null;

			#region 打开提交对话框
			//读取文件状态
			var assemblyList = GetAssemblyInfo();
			var status = new Collection<CommitFileStatus>();
			foreach(var item in entries)
			{
				switch(item.State)
				{
				case FileStatus.NewInWorkdir:
				case FileStatus.ModifiedInWorkdir:
				case FileStatus.TypeChangeInWorkdir:
				case FileStatus.RenamedInWorkdir:
				case FileStatus.DeletedFromWorkdir:
					status.Add(new CommitFileStatus() { FilePath = item.FilePath, FileStatus = item.State });
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
			var versionInfo = ReadVersionInfo() ?? new VersionInfo();
			versionInfo.KeyWords ??= new ObservableCollection<VersionInfo.StringProperty>();

			var instance = Application.Current.MainWindow as MainWindow;
			if(CommitWindow.ShowWindow(instance, status, versionInfo) != true)
				return false;

			WriteVersionInfo(versionInfo);  //先将提交信息写入文件,以备提交失败时自动填入.
			#endregion

			#region 同步上游分支
			if(!repo.Head.IsTracking)
			{
				if(MessageBox.Show("当前为只读版本,是否撤销全部更改?", repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(repo.Head.Tip.Id))?.FriendlyName + " 提交失败", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					repo.Reset(ResetMode.Hard);
					return true;
				}
				return false;
			}

			if(!Git.FetchHead(instance, repo))
				return false;
			#endregion

			#region 更新版本信息
			if(string.Equals(repo.Head.FriendlyName, "master")) //maser分支更新次版本号
			{
				versionInfo.VersionNow = versionInfo.VersionNow == null ? new System.Version(1, 0, 0, 0) : new System.Version(versionInfo.VersionNow.Major, versionInfo.VersionNow.Minor + 1, 0, 0);

				versionInfo.VersionList = new List<VersionInfo.StringPair>();
				foreach(var assembly in assemblyList)
				{
					assembly.HitVersion(-1);
					versionInfo.VersionList.Add(new VersionInfo.StringPair() { Label = Path.GetFileName(assembly.ProjectPath), Value = assembly.Version.ToString() });
				}
			}
			else //其它分支更新修订号
			{
				_ = System.Version.TryParse(repo.Head.FriendlyName, out var branchVersion);
				versionInfo.VersionNow = versionInfo.VersionNow == null ? branchVersion ?? new System.Version(1, 0, 0, 0) : new System.Version(versionInfo.VersionNow.Major, versionInfo.VersionNow.Minor, versionInfo.VersionNow.Build, versionInfo.VersionNow.Revision + 1);

				versionInfo.VersionList = new List<VersionInfo.StringPair>();
				foreach(var assembly in assemblyList)
				{
					assembly.HitVersion(branchVersion == null ? 0 : branchVersion.Build);
					versionInfo.VersionList.Add(new VersionInfo.StringPair() { Label = Path.GetFileName(assembly.ProjectPath), Value = assembly.Version.ToString() });
				}
			}

			var commitText = versionInfo.LatestMessage;
			versionInfo.LatestMessage = null; //不提交最近信息
			WriteVersionInfo(versionInfo);
			#endregion

			#region 提交
			if(!Git.Commit(instance, repo, versionInfo.VersionNow.ToString() + " " + commitText))
				return false;

			//更新界面显示
			var commit = repo.Head.Tip;
			var name = repo.Head.FriendlyName;
			var info = instance._branchInfos.FirstOrDefault(p => p.Name.Equals(name, StringComparison.Ordinal));
			if(info == null) //界面列表不存在此项,则新增一行
			{
				if(string.Equals(name, "master")) //master分支上传Tag
				{
					name = versionInfo.VersionNow.ToString(3);
					ProgressWindow.Show(instance, () => repo.Network.Push(repo.Network.Remotes["origin"], repo.ApplyTag(name).ToString(), Git.GitPushOptions));
					info = new BranchInfo { Type = Git.Type.Tag, Name = name, Version = new System.Version(name), Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort };
					instance._branchInfos.Add(info);
				}
				else if(System.Version.TryParse(name, out var version)) //版本分支在界面新增一行; 非版本分支界面不更新
				{
					info = new BranchInfo { Type = Git.Type.Branch, Name = name, Version = version, Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort };
					instance._branchInfos.Add(info);
				}
			}
			else //界面列表存在此项,则更新显示
			{
				info.Sha = commit.Sha;
				info.Author = commit.Author.Name;
				info.When = commit.Author.When;
				info.Message = commit.MessageShort;
			}
			#endregion

			return true;
		}

		/// <summary>
		/// 创建新分支
		/// </summary>
		/// <param name="info">分支信息</param>
		public static void CreateBranch(BranchInfo info)
		{
			#region 确认状态
			if(info == null)
				return;

			using var repo = new Repository(Settings.LoaclRepoPath);
			var entries = repo.RetrieveStatus();
			if(entries.IsDirty)
			{
				MessageBox.Show("当前版本已修改,请提交或撤销更改后重试!", "版本冲突");
				return;
			}

			//填写提交信息
			var versionInfo = ReadVersionInfo(info.Sha) ?? new VersionInfo();
			versionInfo.KeyWords ??= new ObservableCollection<VersionInfo.StringProperty>();
			versionInfo.VersionBase = versionInfo.VersionNow;
			versionInfo.VersionNow = null;

			var instance = Application.Current.MainWindow as MainWindow;
			if(CommitWindow.ShowWindow(instance, null, versionInfo) != true)
				return;

			if(!Git.FetchHead(instance, repo))
				return;
			#endregion

			#region 更新版本信息
			var build = repo.Branches.Max((o) =>
			{
				if(o.IsRemote && System.Version.TryParse(o.FriendlyName.Split('/').Last(), out var ver) && ver.Major == info.Version.Major && ver.Minor == info.Version.Minor)
					return ver.Build;
				return 0;
			}) + 1; //计算当前版本定制号
			var version = new System.Version(info.Version.Major, info.Version.Minor, build, 0);
			var name = version.ToString(3);
			if(repo.Branches[name] != null)
			{
				repo.Checkout(info.Sha);
			}

			//创建新分支
			var branch = repo.Branches.Add(name, info.Sha, true);
			repo.Checkout(branch);
			repo.Branches.Update(branch, (s) => { s.TrackedBranch = "refs/remotes/origin/" + name; });

			//更新版本信息
			versionInfo.VersionNow = version;
			var commitText = versionInfo.LatestMessage;
			versionInfo.LatestMessage = null; //不提交最近信息
			WriteVersionInfo(versionInfo);
			#endregion

			#region 提交
			if(!Git.Commit(instance, repo, versionInfo.VersionNow.ToString() + " " + commitText))
			{
				repo.Checkout(info.Sha);
				repo.Branches.Remove(branch);
			}
			instance?.UpdateBranchInfo();
			#endregion
		}

		public static void ArchiveCommit(BranchInfo info)
		{
			if(info == null)
				return;

			ProgressWindow.Show(Application.Current.MainWindow, delegate
			{
				using var repo = new Repository(Settings.LoaclRepoPath);
				var cmt = repo.Lookup<Commit>(info.Sha);
				var version = ReadVersionInfo(cmt)?.VersionNow?.ToString();
				var path = Settings.PackageFolder + (version ?? info.Name) + info.Author + "\\";
				WriteFile(path, cmt.Tree);
				Process.Start("explorer", "\"" + path + "\"");

				static void WriteFile(string path, Tree tree)
				{
					if(tree == null)
						return;

					Directory.CreateDirectory(path);
					foreach(var item in tree)
					{
						switch(item.TargetType)
						{
						case TreeEntryTargetType.Blob:
							{
								using var stream = (item.Target as Blob).GetContentStream(new FilteringOptions(".gitattributes"));
								var bytes = new byte[stream.Length];
								stream.Read(bytes, 0, bytes.Length);
								File.WriteAllBytes(path + item.Name, bytes);
							}
							break;
						case TreeEntryTargetType.Tree:
							WriteFile(path + item.Name + "/", item.Target as Tree);
							break;
						case TreeEntryTargetType.GitLink:
							break;
						default:
							break;
						}
					}
				}
			});
		}

		/// <summary>
		/// 更新界面标题
		/// </summary>
		public static void UpdateTitle()
		{
			using var repo = new Repository(Settings.LoaclRepoPath);
			Application.Current.MainWindow.Title = "版本管理 " + (repo.Head.IsTracking ? "[" + repo.Head.FriendlyName + "] " : string.Empty) + repo.Head.Tip?.MessageShort;
		}
		#endregion

		#region 私有方法
		private DelegateCommand TaskbarCmd() => new DelegateCommand((parameter) =>
		{
			Visibility = Visibility.Visible;
			WindowState = WindowState.Maximized;
			Activate();
		});

		/// <summary>
		/// 列举与提交关联的所有记录
		/// </summary>
		/// <param name="name">名称</param>
		/// <param name="version">版本</param>
		/// <param name="commit">Git提交标识</param>
		/// <param name="infos">版本信息列表</param>
		private static void LookupCommit(string name, System.Version version, Commit commit, Collection<BranchInfo> infos)
		{
			if(commit == null || infos.Count > 1000)
				return;

			if(!infos.Any(info => info.Sha == commit.Sha))
			{
				infos.Add(new BranchInfo { Name = name, Version = version, Type = Git.Type.Sha, Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
				foreach(var item in commit.Parents)
				{
					LookupCommit(name, version, item, infos);
				}
			}
		}

		/// <summary>
		/// 更新界面
		/// </summary>
		private void UpdateBranchInfo()
		{
			using var repo = new Repository(Settings.LoaclRepoPath);
			//更新分支列表
			_branchInfos.Clear();
			foreach(var tag in repo.Tags)
			{
				if(!(tag.Target is Commit commit))
					continue;

				var name = tag.FriendlyName;
				if(!System.Version.TryParse(name, out var version))
					continue;

				_branchInfos.Add(new BranchInfo { Type = Git.Type.Tag, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
			}

			foreach(var branch in repo.Branches.Where(p => p.IsRemote))
			{
				var commit = branch.Tip;
				var name = branch.FriendlyName.Split('/').Last();
				if(commit == null || !System.Version.TryParse(name, out var version))
					continue;

				_branchInfos.Add(new BranchInfo { Type = Git.Type.Branch, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
			}

			UpdateTitle();
			BranchInfoGrid.DataContext = _branchInfos;  //在界面显示前,设定上下文
		}

		/// <summary>
		/// 显示设置界面
		/// </summary>
		private void ShowSetWindow()
		{
			var window = new SettingWindow() { Owner = IsLoaded ? this : null, ShowInTaskbar = !IsLoaded };
			window.TopPannel.DataContext = Settings;
			window.ShowDialog();

			Settings.PackageFolder = Settings.PackageFolder.Last() == '\\' ? Settings.PackageFolder : Settings.PackageFolder + "\\";
			Settings.LoaclRepoPath = Settings.LoaclRepoPath.Last() == '\\' ? Settings.LoaclRepoPath : Settings.LoaclRepoPath + "\\";
			if(!Settings.RepoPathList.Contains(Settings.LoaclRepoPath))
			{
				Settings.RepoPathList.Add(Settings.LoaclRepoPath);
			}
			WriteObject(SetFilePath, Settings);
		}
		#endregion

		#region 事件方法
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//分组和排序显示, GetDefaultView概率为null,改为直接操作Items
			BranchInfoGrid.UpdateLayout();  //分组显示前,先更新布局,防止分组折叠后,部分列显示不完整.
			BranchInfoGrid.Items.GroupDescriptions.Add(new PropertyGroupDescription("Version", new VersionConverter()));
			BranchInfoGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Version", System.ComponentModel.ListSortDirection.Ascending));
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			using var repo = new Repository(Settings.LoaclRepoPath);
			if(repo != null && repo.RetrieveStatus().IsDirty)
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

		private void Open_Click(object sender, RoutedEventArgs e) => Process.Start(Directory.GetFiles(Settings.LoaclRepoPath, "*.sln", SearchOption.AllDirectories).FirstOrDefault() ?? Settings.LoaclRepoPath);

		private void Explorer_Click(object sender, RoutedEventArgs e) => Process.Start(Settings.LoaclRepoPath);

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			using var repo = new Repository(Settings.LoaclRepoPath);
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
			using(var repo = new Repository(Settings.LoaclRepoPath))
			{
				if(Commit(repo) == false)
					return;
			}

			ProgressWindow.Show(this, delegate
			{
				var version = ReadVersionInfo()?.VersionNow?.ToString();
				var folder = Path.Combine(Settings.PackageFolder, version + "\\");
				Directory.CreateDirectory(folder);

				//生成解决方案
				foreach(var item in Directory.GetFiles(Settings.LoaclRepoPath, "*.sln", SearchOption.AllDirectories))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = Settings.MSBuildPath,
						Arguments = "/t:publish /p:Configuration=Release /noconsolelogger \"" + item + "\"",
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}).WaitForExit();
				}

				//生成自解压安装包
				foreach(var item in Directory.GetFiles(Settings.LoaclRepoPath, "setup.exe", SearchOption.AllDirectories))
				{
					var dir = Path.GetDirectoryName(item);
					var app = Directory.GetFiles(dir, "*.application").FirstOrDefault();
					if(string.IsNullOrEmpty(app))
						continue;

					var rarPath = Path.Combine(Environment.CurrentDirectory, "Package\\");
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine(rarPath, "WinRAR.exe"),
						Arguments = string.Format("a -r -s -sfx -z{0} -iicon{1} -iadm -ibck \"{2}\"", rarPath + "sfx", rarPath + "msi.ico", Path.Combine(folder, Path.GetFileNameWithoutExtension(app) + " v" + version)),
						WorkingDirectory = dir,
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}).WaitForExit();
				}

				//复制hex文件
				foreach(var item in Directory.GetFiles(Settings.LoaclRepoPath, "*.hex", SearchOption.AllDirectories))
				{
					File.Copy(item, Path.Combine(folder, Path.GetFileName(item)), true);
				}

				//复制bin文件
				foreach(var item in Directory.GetFiles(Settings.LoaclRepoPath, "*.bin", SearchOption.AllDirectories))
				{
					File.Copy(item, Path.Combine(folder, Path.GetFileName(item)), true);
				}
				Process.Start(Settings.PackageFolder);
			});
		}

		private void Set_Click(object sender, RoutedEventArgs e)
		{
			ShowSetWindow();
			ProgressWindow.Show(this, Git.Sync, UpdateBranchInfo);
		}
		#endregion
	}
}
