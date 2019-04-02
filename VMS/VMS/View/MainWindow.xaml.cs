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
		const string DIR_WORK = @".\";
		const string DIR_DOC = DIR_WORK + @"Doc\";
		const string FILE_SETTING = DIR_WORK + "Setting.json";
		static Setting setting;

		public MainWindow()
		{
			InitializeComponent();
			Title = "版本管理系统 v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

			IsEnabled = false;
			var initWorker = new BackgroundWorker();
			initWorker.DoWork += InitWorker_DoWork;
			initWorker.RunWorkerCompleted += InitWorker_RunWorkerCompleted;
			initWorker.RunWorkerAsync();
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

		private void InitWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			var js = new JavaScriptSerializer();
			try
			{
				setting = js.Deserialize<Setting>(File.ReadAllText(FILE_SETTING));
			}
			catch(Exception)
			{ }

			if(setting == null)
			{
				setting = new Setting
				{
					User = "user",
					RepoUrl = @"http://admin:admin@192.168.120.129:2507/r/Test.git",
					LoaclRepoPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VMS\",
					CompareToolPath = @"D:\Program Files\Beyond Compare 4\BCompare.exe"
				};
				File.WriteAllText(FILE_SETTING, js.Serialize(setting));
			}
			setting.LoaclRepoPath = setting.LoaclRepoPath.Last() == '\\' ? setting.LoaclRepoPath : setting.LoaclRepoPath + "\\";

			if(!Directory.Exists(setting.LoaclRepoPath))
			{
				Repository.Clone(setting.RepoUrl, setting.LoaclRepoPath);
			}

			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				try
				{
					Commands.Fetch(repo, "origin", new string[0], null, null);
				}
				catch(Exception x)
				{
					Dispatcher.Invoke(delegate { MessageBox.Show(this, x.Message, "同步失败!"); });
				}
			}
		}

		private void InitWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				foreach(var branch in repo.Branches.Where(p => p.IsRemote))
				{
					var name = branch.FriendlyName.Split('/').Last();
					if(System.Version.TryParse(name, out System.Version version))
					{
						var panel = new DockPanel();
						var text = new TextBlock { Text = name };
						var newBranch = new Button { Background = null, Margin = new Thickness(12, 0, 0, 0), BorderThickness = new Thickness(), Content = new Image() { Height = 32, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri("pack://SiteOfOrigin:,,,/Images/Add.png")) }, ToolTip = "基于此版本新建" };
						var openBranch = new Button { Background = null, Margin = new Thickness(12, 0, 0, 0), BorderThickness = new Thickness(), Content = new Image() { Height = 32, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri("pack://SiteOfOrigin:,,,/Images/Checkout.png")) }, ToolTip = "切换到此版本" };
						var item = new ListBoxItem() { Content = panel, HorizontalContentAlignment = HorizontalAlignment.Stretch };

						var sha = branch.Tip.Sha;

						//var b = Commands.Checkout(repo, sha);
						openBranch.Click += delegate
						{
							Operate.Checkout(setting.LoaclRepoPath, name);
						};
						DockPanel.SetDock(newBranch, Dock.Right);
						DockPanel.SetDock(openBranch, Dock.Right);
						panel.Children.Add(newBranch);
						panel.Children.Add(openBranch);
						panel.Children.Add(text);
						var index = GitList.Items.Add(item);
					}
				}

				//foreach(var item in repo.Branches.Where(p => !p.IsRemote))
				//{
				//	//Commands.Checkout(repo, item);
				//	GitList.Items.Add(repo.Head.FriendlyName);
				//}

				foreach(var tag in repo.Tags)
				{
					if(System.Version.TryParse(tag.FriendlyName, out System.Version version))
					{
						UserList.Items.Add(tag.FriendlyName);
					}
				}

				//repo.Tags.Add(;

				//repo.Branches.Add

				//repo.Network.Push();
				GitList.Items.Add("Commit:");
				GitList.Items.Add(repo.Head.FriendlyName);
				var cmt = repo.Head.Tip;
				GitList.Items.Add(cmt.Committer.Name);


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

			IsEnabled = true;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(entries.IsDirty)
				{
					List<string> versions = new List<string>();
					var allProperties = Directory.GetDirectories(setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories);
					for(int i = 0; i < allProperties.Length; i++)
					{
						allProperties[i] = System.IO.Path.GetDirectoryName(allProperties[i]).Substring(setting.LoaclRepoPath.Length + 1).Replace('\\', '/');
					}

					foreach(var item in entries)
					{
						switch(item.State)
						{
						case FileStatus.NewInWorkdir:
						case FileStatus.ModifiedInWorkdir:
						case FileStatus.DeletedFromWorkdir:
						case FileStatus.TypeChangeInWorkdir:
						case FileStatus.RenamedInWorkdir:
							GitList.Items.Add(item.FilePath + " - " + item.State.ToString());
							repo.Index.Add(item.FilePath);

							foreach(var path in allProperties)
							{
								if(item.FilePath.Contains(path))
								{
									var file = path + "/Properties/AssemblyInfo.cs";
									if(!versions.Contains(file))
									{
										versions.Add(file);
									}
								}
							}
							break;
						default:
							break;
						}
					}

					//foreach(var item in versions)
					//{
					//	if(AddVersion(setting.LoaclRepoPath + "/" + item))
					//	{
					//		repo.Index.Add(item);
					//	}
					//}

					//repo.Index.Write();
					//var signa = new Signature(setting.User, Environment.MachineName, DateTime.Now);
					//repo.Commit("自动提交", signa, signa);
					//repo.Network.Push(repo.Head);
				}
			}
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
		/// 上传时,自动升级版本号
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		bool AddVersion(string file, out string newVersion)
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
		/// 提交新版本
		/// </summary>
		/// <returns>提交成功, true: 否则,false</returns>
		bool Commit()
		{
			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(!entries.IsDirty)
					return true;

				List<string> verFiles = new List<string>(); //AssemblyInfo文件的相对路径
				var allProperties = Directory.GetDirectories(setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories);	//版本配置文件所在目录的相对路径
				for(int i = 0; i < allProperties.Length; i++)
				{
					allProperties[i] = System.IO.Path.GetDirectoryName(allProperties[i]).Substring(setting.LoaclRepoPath.Length).Replace('\\', '/');
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
					case FileStatus.DeletedFromWorkdir:
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
					if(AddVersion(setting.LoaclRepoPath + item, out string ver))
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
				repo.Index.Write();
				var sign = new Signature(setting.User, Environment.MachineName, DateTime.Now);
				repo.Commit(cmtText.ToString(), sign, sign);
				repo.Network.Push(repo.Head);
			}
			return true;
		}

		private void Open_Click(object sender, RoutedEventArgs e)
		{
			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				var prj = Directory.GetFiles(setting.LoaclRepoPath, "*.sln", SearchOption.AllDirectories);
				if(prj.Length > 0)
				{
					Process.Start(prj[0]);
				}
			}
		}

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			Commit();
		}

		private void Package_Click(object sender, RoutedEventArgs e)
		{

		}

		private void Set_Click(object sender, RoutedEventArgs e)
		{
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
					using(var repo = new Repository(setting.LoaclRepoPath))
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
							Process.Start(setting.CompareToolPath, " \"" + filePath + "\" \"" + setting.LoaclRepoPath + info.FilePath + "\"");
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
