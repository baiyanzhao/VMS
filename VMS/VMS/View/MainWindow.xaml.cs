using LibGit2Sharp;
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
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Web.Script.Serialization;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace VMS
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		static Setting _setting;
		const string FILE_SETTING = ".\\Setting.json";

		public MainWindow()
		{
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

		private void Init()
		{
			try
			{
				_setting = new JavaScriptSerializer().Deserialize<Setting>(File.ReadAllText(FILE_SETTING));
			}
			catch(Exception)
			{ }

			if(_setting == null)
			{
				_setting = new Setting
				{
					RepoUrl = @"http://admin:admin@192.168.1.49:2507/r/Test.git",
					LoaclRepoPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VMS\",
					CompareToolPath = @"D:\Program Files\Beyond Compare 4\BCompare.exe"
				};
				Dispatcher.Invoke(delegate { ShowSetWindow(); });
			}

			if(!Directory.Exists(_setting.LoaclRepoPath))
			{
				Repository.Clone(_setting.RepoUrl, _setting.LoaclRepoPath);
			}

			try
			{
				using(var repo = new Repository(_setting.LoaclRepoPath))
				{
					Commands.Fetch(repo, "origin", new string[0], null, null);
				}
			}
			catch(Exception x)
			{
				Dispatcher.Invoke(delegate { MessageBox.Show(this, x.Message, "同步失败!"); });
			}
			
		}

		private void InitCompleted()
		{
			using(var repo = new Repository(_setting.LoaclRepoPath))
			{
				foreach(var tag in repo.Tags)
				{
					var name = tag.FriendlyName;
					if(System.Version.TryParse(name, out System.Version version))
					{
						General.Items.Add(CreateButtonItem(name, tag.Target.Sha));
					}
				}

				foreach(var branch in repo.Branches.Where(p => p.IsRemote))
				{
					var name = branch.FriendlyName.Split('/').Last();
					if(System.Version.TryParse(name, out System.Version version))
					{
						Special.Items.Add(CreateButtonItem(name, name));
					}
				}

				//repo.Tags.Add(;

				//repo.Branches.Add

				//repo.Network.Push();
				Special.Items.Add("Commit:");
				Special.Items.Add(repo.Head.FriendlyName);
				var cmt = repo.Head.Tip;
				Special.Items.Add(cmt.Committer.Name);


				// Object lookup
				//var obj = repo.Lookup("sha");
				//var commit = repo.Lookup<Commit>("sha");
				//var tree = repo.Lookup<Tree>("sha");
				//var tag = repo.Lookup<Tag>("sha");

				//// Rev walking
				//foreach(var c in repo.Commits.Walk("sha")) { }
				//var commits = repo.Commits.StartingAt("sha").Where(c => c).ToList();
				//var sortedCommits = repo.Commits.StartingAt("sha").SortBy(SortMode.Topo).ToList();

				//// Refs
				//var reference = repo.Refs["refs/heads/master"];
				//var allRefs = repo.Refs.ToList();
				//foreach(var c in repo.Refs["HEAD"].Commits) { }
				//foreach(var c in repo.Head.Commits) { }
				//var headCommit = repo.Head.Commits.First();
				//var allCommits = repo.Refs["HEAD"].Commits.ToList();
				//var newRef = repo.Refs.CreateFrom(reference);
				//var anotherNewRef = repo.Refs.CreateFrom("sha");

				//// Branches
				//// special kind of reference
				//var allBranches = repo.Branches.ToList();
				//var branch = repo.Branches["master"];
				//var remoteBranch = repo.Branches["origin/master"];
				//var localBranches = repo.Branches.Where(p => p.Type == BranchType.Local).ToList();
				//var remoteBranches = repo.Branches.Where(p => p.Type == BranchType.Remote).ToList();
				//var newBranch = repo.Branches.CreateFrom("sha");
				//var anotherNewBranch = repo.Branches.CreateFrom(newBranch);
				//repo.Branches.Delete(anotherNewBranch);

				//// Tags
				//// really another special kind of reference
				//var aTag = repo.Tags["refs/tags/v1.0"];
				//var allTags = repo.Tags.ToList();
				//var newTag = repo.Tags.CreateFrom("sha");
				//var newTag2 = repo.Tags.CreateFrom(commit);
				//var newTag3 = repo.Tags.CreateFrom(reference);
			}
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
		/// 签出版本
		/// </summary>
		/// <param name="committishOrBranchSpec"></param>
		private void Checkout(string committishOrBranchSpec)
		{
			if(Operate.Checkout(_setting.LoaclRepoPath, committishOrBranchSpec))
			{
				UpdateBranchInfo();
			}
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>提交成功, true: 否则,false</returns>
		private bool Commit()
		{
			using(var repo = new Repository(_setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(!entries.IsDirty || !repo.Head.IsTracking)
					return true;

				List<string> verFiles = new List<string>(); //AssemblyInfo文件的相对路径
				var allProperties = Directory.GetDirectories(_setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories);	//版本配置文件所在目录的相对路径
				for(int i = 0; i < allProperties.Length; i++)
				{
					allProperties[i] = System.IO.Path.GetDirectoryName(allProperties[i]).Substring(_setting.LoaclRepoPath.Length).Replace('\\', '/');
				}

				var infos = new ObservableCollection<CommitInfo>();	//文件信息
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
						infos.Add(new CommitInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
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
						infos.Add(new CommitInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
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
					if(AddVersion(_setting.LoaclRepoPath + item, out string ver))
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
					var sign = new Signature(_setting.User, Environment.MachineName, DateTime.Now);
					repo.Commit(cmtText.ToString(), sign, sign);
					repo.Network.Push(repo.Head);
				});
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
			setWindow.TopPannel.DataContext = _setting;
			setWindow.ShowDialog();
			_setting.LoaclRepoPath = _setting.LoaclRepoPath.Last() == '\\' ? _setting.LoaclRepoPath : _setting.LoaclRepoPath + "\\";
			File.WriteAllText(FILE_SETTING, new JavaScriptSerializer().Serialize(_setting));
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			ProgressWindow.Show(this, Init, InitCompleted);
		}

		private void Open_Click(object sender, RoutedEventArgs e)
		{
			var prj = Directory.GetFiles(_setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories);
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
				foreach(var item in Directory.GetFiles(_setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories))
				{
					var p = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
							Arguments = "/t:publish /p:Configuration=Release /noconsolelogger \"" + item + "\"",
							CreateNoWindow = true,
							WindowStyle = ProcessWindowStyle.Hidden
						}
					};
					p.Start();
					p.WaitForExit();
				}
			});


		}

		private void Set_Click(object sender, RoutedEventArgs e)
		{
			ShowSetWindow();
		}

		/// <summary>
		/// Git提交信息
		/// </summary>
		class CommitInfo
		{
			public string FilePath { get; set; }
			public string State { get; set; }
			public ICommand Diff
			{
				get
				{
					if(_diff == null)
					{
						_diff = new DiffCommand();
					}
					return _diff;
				}
			}

			private ICommand _diff;
			class DiffCommand : ICommand
			{
				public event EventHandler CanExecuteChanged;
				public bool CanExecute(object parameter)
				{
					return true;
				}

				public void Execute(object parameter)
				{
					using(var repo = new Repository(_setting.LoaclRepoPath))
					{
						var info = parameter as CommitInfo;
						var tree = repo.Index.WriteToTree();
						var blob = tree[info.FilePath]?.Target as Blob;
						if(info == null || blob == null)
							return;

						try
						{
							var filePath = Path.GetTempFileName();
							File.WriteAllText(filePath, blob.GetContentText());
							File.SetAttributes(filePath, FileAttributes.ReadOnly | FileAttributes.Temporary);
							Process.Start(_setting.CompareToolPath, " \"" + filePath + "\" \"" + _setting.LoaclRepoPath + info.FilePath + "\"");
						}
						catch(Exception x)
						{
							MessageBox.Show(x.Message);
						}
					}
				}

				public void RaiseCanExecuteChanged()
				{
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
	}
}
