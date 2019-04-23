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
using System.Windows.Data;
using VMS.Model;
using VMS.ViewModel;
using static VMS.Operate;

namespace VMS
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		static MainWindow Instance;
		static BranchListView _branchInfos = new BranchListView(); //分支信息

		public MainWindow()
		{
			Instance = this;
			InitializeComponent();
			Title = "源程序版本管理系统 v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
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

		/// <summary>
		/// 绑定分支界面
		/// </summary>
		private void BindingBranchInfo()
		{
			BranchInfoGrid.DataContext = _branchInfos;
			var view = CollectionViewSource.GetDefaultView(BranchInfoGrid.ItemsSource);
			if(view != null)
			{
				view.GroupDescriptions.Clear();
				view.GroupDescriptions.Add(new PropertyGroupDescription("Version", new VersionConverter()));

				view.SortDescriptions.Clear();
				view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Version", System.ComponentModel.ListSortDirection.Ascending));
			}
		}

		/// <summary>
		/// 更新界面
		/// </summary>
		private void UpdateBranchInfo()
		{
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				//更新分支列表
				_branchInfos.Clear();
				foreach(var tag in repo.Tags)
				{
					if(!(tag.Target is Commit commit))
						continue;

					var name = tag.FriendlyName;
					if(!System.Version.TryParse(name, out System.Version version))
						continue;

					_branchInfos.Add(new BranchInfo { Type = GitType.Tag, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
				}

				foreach(var branch in repo.Branches.Where(p => p.IsRemote))
				{
					var commit = branch.Tip;
					var name = branch.FriendlyName.Split('/').Last();
					if(commit == null || !System.Version.TryParse(name, out System.Version version))
						continue;

					_branchInfos.Add(new BranchInfo { Type = GitType.Branch, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
				}

				_branchInfos.HeadName = (repo.Head.IsTracking) ? repo.Head.FriendlyName : repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(repo.Head.Tip.Id))?.FriendlyName;   //Head为分支则显示分支名称,否则显示Tag名称
			}
		}

		/// <summary>
		/// 显示设置界面
		/// </summary>
		private void ShowSetWindow()
		{
			var setWindow = new SettingWindow() { Owner = this };
			setWindow.TopPannel.DataContext = Global.Setting;
			setWindow.ShowDialog();
			Global.Setting.LoaclRepoPath = Global.Setting.LoaclRepoPath.Last() == '\\' ? Global.Setting.LoaclRepoPath : Global.Setting.LoaclRepoPath + "\\";
			File.WriteAllText(Global.FILE_SETTING, new JavaScriptSerializer().Serialize(Global.Setting));
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>工程未更改或提交成功, true: 否则,false</returns>
		public static bool Commit()
		{
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(!entries.IsDirty)
					return true;

				if(!repo.Head.IsTracking)
				{
					if(MessageBox.Show("当前为只读版本,是否撤销全部更改?", "禁用提交!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						repo.Reset(ResetMode.Hard);
						return true;
					}
					return false;
				}

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
				var versionInfo = Global.ReadVersionInfo();
				versionInfo.KeyWords = versionInfo.KeyWords ?? new ObservableCollection<VersionInfo.StringProperty>();
				var commitText = CommitWindow.ShowWindow(Instance, status, versionInfo);
				if(commitText == null)
					return false;

				//更新版本信息
				System.Version.TryParse(repo.Head.FriendlyName, out System.Version branchVersion);
				versionInfo.VersionNow = versionInfo.VersionNow == null ? branchVersion ?? new System.Version(1, 0, 0, 0) : new System.Version(versionInfo.VersionNow.Major, versionInfo.VersionNow.Minor, versionInfo.VersionNow.Build, versionInfo.VersionNow.Revision + 1);

				versionInfo.VersionList = new List<VersionInfo.StringPair>();
				foreach(var assembly in assemblyList)
				{
					assembly.HitVersion(branchVersion == null ? 0 : branchVersion.Build);
					versionInfo.VersionList.Add(new VersionInfo.StringPair() { Label = Path.GetFileName(assembly.ProjectPath), Value = assembly.Version.ToString() });
				}
				Global.WriteVersionInfo(versionInfo);

				//提交
				ProgressWindow.Show(Instance, delegate
				{
					try
					{
						Global.Git.Commit(repo, versionInfo.VersionNow.ToString() + " " + commitText);
					}
					catch(Exception x)
					{
						Instance.Dispatcher.Invoke(delegate { MessageBox.Show(Instance, x.Message, "推送失败,将在下次启动时尝试推送!", MessageBoxButton.OK, MessageBoxImage.Error); });
					}

				},
				delegate
				{
					var commit = repo.Head.Tip;
					var name = repo.Head.FriendlyName;
					var info = _branchInfos.FirstOrDefault(p => p.Name.Equals(name));
					if(info == null)
					{
						if(!System.Version.TryParse(name, out System.Version version))
							return;

						info = new BranchInfo { Type = GitType.Branch, Name = name, Version = version, Sha = commit.Sha, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort };
						_branchInfos.Add(info);
					}
					else
					{
						info.Sha = commit.Sha;
						info.Author = commit.Author.Name;
						info.When = commit.Author.When;
						info.Message = commit.MessageShort;
					}
				});
			}
			return true;
		}

		/// <summary>
		/// 签出版本
		/// </summary>
		public static void Checkout(string mark, Operate.GitType type)
		{
			if(Operate.Checkout(Global.Setting.LoaclRepoPath, mark, type))
			{
				Instance.UpdateBranchInfo();
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if(Global.Setting.User == null)
			{
				ShowSetWindow();
			}
			Directory.CreateDirectory(Global.Setting.PackageFolder);
			ProgressWindow.Show(this, Global.Git.Sync, UpdateBranchInfo);
			BindingBranchInfo();
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
			Commit();
		}

		private void Package_Click(object sender, RoutedEventArgs e)
		{
			if(!Commit())
				return;

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
