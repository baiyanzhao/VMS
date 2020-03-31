using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
		#region 定义
		private readonly TaskbarIcon _taskbar = new TaskbarIcon { Visibility = Visibility.Hidden }; //通知区图标
		private readonly FileExportFilter _lfsFilter = new FileExportFilter("lfs-filter", new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });  //Git-LFS筛选器
		#endregion

		#region 公共方法
		public MainWindow()
		{
			InitializeComponent();
			var taskbarCmd = new DelegateCommand((parameter) =>
			{
				Visibility = Visibility.Visible;
				WindowState = WindowState.Maximized;
				Activate();
			});

			_taskbar.IconSource = Icon;
			_taskbar.LeftClickCommand = taskbarCmd;
			_taskbar.DoubleClickCommand = taskbarCmd;
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
					_taskbar.ShowBalloonTip(Title, "进入后台运行", BalloonIcon.None);
					break;
				case WindowState.Maximized:
					_taskbar.Visibility = Visibility.Hidden;
					break;
				default:
					break;
				}
			};

			RepoTab.DataContext = RepoData;
			GlobalSettings.RegisterFilter(_lfsFilter);
			Directory.CreateDirectory(Settings.PackageFolder);
		}

		~MainWindow()
		{
			//清理临时文件
			foreach(var item in Directory.GetFiles(Path.GetTempPath(), "vms@*", SearchOption.TopDirectoryOnly))
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
			_lfsFilter.Dispose();
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
			using(var repo = new Repository(LocalRepoPath))
			{
				LookupCommit(name, version, repo.Lookup<Commit>(sha), infos);
			}

			new LogWindow { Owner = Application.Current.MainWindow, Title = name, DataContext = infos.OrderByDescending(info => info.When) }.ShowDialog();
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>工程未更改, null; 提交成功, true: 否则,false</returns>
		public static bool? Commit()
		{
			using var repo = new Repository(LocalRepoPath);
			var entries = repo.RetrieveStatus();
			if(!entries.IsDirty)
				return null;

			#region 打开提交对话框
			var assemblyList = AssemblyInfo.GetInfos(LocalRepoPath); //读取文件状态
			var status = new ObservableCollection<CommitFileStatus>();
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
						if(assembly.IsModified == false && item.FilePath.Replace('\\', '/').Contains(assembly.ProjectPath))
						{
							assembly.IsModified = true;
							break;
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
			var commitWindow = new CommitWindow() { Owner = instance, Title = RepoData.CurrentRepo?.Title };
			commitWindow.FileGrid.DataContext = status;
			commitWindow.Version.DataContext = versionInfo;
			commitWindow.Info.Text = status?.Count.ToString();
			if(commitWindow.ShowDialog() != true)
				return false;
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

				versionInfo.VersionList = new List<VersionInfo.VersionProperty>();
				foreach(var assembly in assemblyList)
				{
					assembly.HitVersion(-1);
					versionInfo.VersionList.Add(new VersionInfo.VersionProperty() { Label = Path.GetFileName(assembly.ProjectPath), Title = assembly.Title, Value = assembly.Version.ToString() });
				}
			}
			else //其它分支更新修订号
			{
				_ = System.Version.TryParse(repo.Head.FriendlyName, out var branchVersion);
				versionInfo.VersionNow = versionInfo.VersionNow == null ? branchVersion ?? new System.Version(1, 0, 0, 0) : new System.Version(versionInfo.VersionNow.Major, versionInfo.VersionNow.Minor, versionInfo.VersionNow.Build, versionInfo.VersionNow.Revision + 1);

				versionInfo.VersionList = new List<VersionInfo.VersionProperty>();
				foreach(var assembly in assemblyList)
				{
					assembly.HitVersion(branchVersion == null ? 0 : branchVersion.Build);
					versionInfo.VersionList.Add(new VersionInfo.VersionProperty() { Label = Path.GetFileName(assembly.ProjectPath), Title = assembly.Title, Value = assembly.Version.ToString() });
				}
			}
			WriteVersionInfo(versionInfo);
			#endregion

			#region 提交
			if(!Git.Commit(instance, repo, versionInfo.VersionNow.ToString() + " " + commitWindow.Message.Text))
				return false;

			RepoData.CurrentRepo?.Update();
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

			using var repo = new Repository(LocalRepoPath);
			if(repo.RetrieveStatus().IsDirty)
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
			var commitWindow = new CommitWindow() { Owner = instance, Title = "基于" + versionInfo.VersionBase + " 新建分支" };
			commitWindow.FileGrid.DataContext = null;
			commitWindow.Version.DataContext = versionInfo;
			commitWindow.Info.Text = null;
			if(commitWindow.ShowDialog() != true)
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
				Commands.Checkout(repo, info.Sha);
			}

			//创建新分支
			var branch = repo.Branches.Add(name, info.Sha, true);
			Commands.Checkout(repo, branch);
			repo.Branches.Update(branch, (s) => { s.TrackedBranch = "refs/remotes/origin/" + name; });

			//更新版本信息
			versionInfo.VersionNow = version;
			WriteVersionInfo(versionInfo);
			#endregion

			#region 提交
			if(!Git.Commit(instance, repo, versionInfo.VersionNow.ToString() + " " + commitWindow.Message.Text))
			{
				Commands.Checkout(repo, info.Sha);
				repo.Branches.Remove(branch);
			}
			RepoData.CurrentRepo?.Update();
			#endregion
		}

		public static void ArchiveCommit(BranchInfo info)
		{
			if(info == null)
				return;

			ProgressWindow.Show(Application.Current.MainWindow, delegate
			{
				using var repo = new Repository(LocalRepoPath);
				var cmt = repo.Lookup<Commit>(info.Sha);
				var version = ReadVersionInfo(cmt)?.VersionNow?.ToString();
				var path = Settings.PackageFolder + (version ?? info.Name) + " " + info.Author + "\\";
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
							using(var stream = (item.Target as Blob).GetContentStream(new FilteringOptions(item.Path)))
							{
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
		#endregion

		#region 私有方法
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
		/// 显示设置界面
		/// </summary>
		private void ShowSetWindow()
		{
			var window = new SettingWindow() { Owner = IsLoaded ? this : null, ShowInTaskbar = !IsLoaded };
			window.TopPannel.DataContext = Settings;
			window.ShowDialog();

			Settings.PackageFolder = Settings.PackageFolder.Last() == '\\' ? Settings.PackageFolder : Settings.PackageFolder + "\\";
			WriteSetting();
		}
		#endregion

		#region 事件方法
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if(Settings.User == null)
			{
				ShowSetWindow();
			}

			if(Settings.RepoPathList.Count <= 0)
			{
				AddRepo_Click(null, null);
			}
			else
			{
				/// 每个仓库独立更新,避免互相影响
				foreach(var item in Settings.RepoPathList)
				{
					var info = new RepoInfo(item);
					RepoData.RepoList.Add(info);
					ProgressWindow.Show(this, () => Git.Sync(item), info.Update);
				}
				RepoData.CurrentRepo ??= RepoData.RepoList.FirstOrDefault();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			foreach(var item in RepoData.RepoList)
			{
				using var repo = new Repository(item.LocalRepoPath);
				if(repo != null && repo.RetrieveStatus().IsDirty)
				{
					switch(MessageBox.Show(Application.Current.MainWindow, "存在尚未提交的文件,是否立即提交?\n 点'是', 提交更改\n 点'否', 直接退出\n 点'取消', 不进行任何操作.", item.Title + " 尚有文件未提交", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
					{
					case MessageBoxResult.Yes:
						RepoData.CurrentRepo = item;
						Commit();
						break;
					case MessageBoxResult.Cancel:
						e.Cancel = true;
						return;
					default:
						return;
					}
				}
			}
		}

		private void Open_Click(object sender, RoutedEventArgs e) => Process.Start(Directory.GetFiles(LocalRepoPath, "*.sln", SearchOption.AllDirectories).FirstOrDefault() ?? LocalRepoPath);

		private void Explorer_Click(object sender, RoutedEventArgs e) => Process.Start(LocalRepoPath);

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			if(Commit() == null)
			{
				MessageBox.Show("当前版本无任何更改!", RepoData.CurrentRepo?.Title);
			}
		}

		/// <summary>
		/// 快速提交全部仓库
		/// </summary>
		private void ImmediateCommit_Click(object sender, RoutedEventArgs e)
		{
			var instance = Application.Current.MainWindow as MainWindow;
			if(MessageBox.Show(instance, "快速提交自动上传当前打开的全部仓库,而不自动更新任何版本信息.\n并在提交完成后关闭计算机！\n\n 确定执行快速提交吗?", "快速提交并关机", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
				return;

			foreach(var item in RepoData.RepoList)
			{
				using var repo = new Repository(item.LocalRepoPath);
				if(repo != null && repo.RetrieveStatus().IsDirty)
				{
					Git.Commit(instance, repo, DateTime.Now.ToString());
					item.Update();
				}
			}

			Close();
			Process.Start("shutdown", @"/s /t 300");
		}

		/// <summary>
		/// 生成安装包
		/// </summary>
		private void Package_Click(object sender, RoutedEventArgs e)
		{
			if(Commit() == false)
				return;

			ProgressWindow.Show(this, delegate
			{
				var version = ReadVersionInfo()?.VersionNow?.ToString();
				var folder = Path.Combine(Settings.PackageFolder, version + "\\");
				Directory.CreateDirectory(folder);

				//生成解决方案
				foreach(var item in Directory.GetFiles(LocalRepoPath, "*.sln", SearchOption.AllDirectories))
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
				foreach(var item in Directory.GetFiles(LocalRepoPath, "setup.exe", SearchOption.AllDirectories))
				{
					var dir = Path.GetDirectoryName(item);
					var name = Path.GetFileNameWithoutExtension(Directory.GetFiles(dir, "*.application").FirstOrDefault()) ?? item.Substring(LocalRepoPath.Length).Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).First();
					if(string.IsNullOrEmpty(name))
						continue;

					var rarPath = Path.Combine(Environment.CurrentDirectory, "Package\\");
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine(rarPath, "WinRAR.exe"),
						Arguments = string.Format("a -r -s -sfx -z{0} -iicon{1} -iadm -ibck \"{2}\"", rarPath + "sfx", rarPath + "msi.ico", Path.Combine(folder, name + " v" + version)),
						WorkingDirectory = dir,
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}).WaitForExit();
				}

				//复制hex文件
				foreach(var item in Directory.GetFiles(LocalRepoPath, "*.hex", SearchOption.AllDirectories))
				{
					File.Copy(item, Path.Combine(folder, Path.GetFileName(item)), true);
				}

				//复制bin文件
				foreach(var item in Directory.GetFiles(LocalRepoPath, "*.bin", SearchOption.AllDirectories))
				{
					File.Copy(item, Path.Combine(folder, Path.GetFileName(item)), true);
				}
				Process.Start(Settings.PackageFolder);
			});
		}

		/// <summary>
		/// 发布标准版
		/// </summary>
		private void Publish_Click(object sender, RoutedEventArgs e)
		{
			#region 确认状态
			using var repo = new Repository(LocalRepoPath);
			if(!string.Equals(repo.Head.FriendlyName, "0.0.0") || repo.RetrieveStatus().IsDirty)
			{
				MessageBox.Show("只能基于纯净的内测版发布", "权限不足");
				return;
			}

			var versionInfo = ReadVersionInfo() ?? new VersionInfo();
			versionInfo.KeyWords ??= new ObservableCollection<VersionInfo.StringProperty>();
			var instance = Application.Current.MainWindow as MainWindow;
			var commitWindow = new CommitWindow() { Owner = instance, Title = RepoData.CurrentRepo?.Title };
			commitWindow.FileGrid.DataContext = null;
			commitWindow.Version.DataContext = versionInfo;
			commitWindow.Info.Text = null;
			if(commitWindow.ShowDialog() != true)
				return;

			if(!Git.FetchHead(instance, repo))
				return;
			#endregion

			#region 提交标准版
			versionInfo.VersionNow = versionInfo.VersionNow == null ? new System.Version(1, 0, 0, 0) : new System.Version(versionInfo.VersionNow.Major, versionInfo.VersionNow.Minor + 1, 0, 0);
			WriteVersionInfo(versionInfo);	//升级版本文件
			Git.Publish(instance, repo, versionInfo.VersionNow.ToString() + " " + commitWindow.Message.Text, versionInfo.VersionNow.ToString(3));
			RepoData.CurrentRepo?.Update();
			#endregion
		}

		private void Set_Click(object sender, RoutedEventArgs e) => ShowSetWindow();

		private void AddRepo_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new FolderBrowserForWPF.Dialog() { Title = "打开[空文件夹]或[本地仓库文件夹]" };
			if(dlg.ShowDialog() != true)
				return;

			var path = dlg.FileName.Last() == '\\' ? dlg.FileName : dlg.FileName + "\\";
			if(Settings.RepoPathList.Contains(path))
				return;

			var info = new RepoInfo(path);
			ProgressWindow.Show(null, () => Git.Sync(path), info.Update);
			{
				RepoData.RepoList.Add(info);
				Settings.RepoPathList.Add(path);
				RepoData.CurrentRepo ??= RepoData.RepoList.FirstOrDefault();
				WriteSetting();
			}
		}

		private void DelRepo_Click(object sender, RoutedEventArgs e)
		{
			if(RepoData.CurrentRepo != null && Settings.RepoPathList.Count > 1)
			{
				Settings.RepoPathList.Remove(RepoData.CurrentRepo.LocalRepoPath);
				RepoData.RepoList.Remove(RepoData.CurrentRepo);
				WriteSetting();
			}
		}

		private void DataGrid_Loaded(object sender, RoutedEventArgs e)
		{
			//数据绑定加载后再分组,防止分组折叠后,部分列显示不完整
			if(sender is DataGrid grid && grid.Items.GroupDescriptions.Count <= 0)
			{
				grid.Items.GroupDescriptions.Add(new PropertyGroupDescription("Version", new VersionConverter()));
				grid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Version", System.ComponentModel.ListSortDirection.Ascending));
			}
		}
		#endregion
	}
}
