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
using System.Windows.Shapes;
using System.Web.Script.Serialization;
using System.IO;

namespace VMS
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		const string DIR_WORK = @".\";
		const string DIR_DOC = DIR_WORK + @"Doc\";
		const string DIR_SYS = DIR_WORK + @"Sys\";
//		const string FILE_PRESET = DIR_SYS + "Preset.json";
		const string FILE_SETTING = DIR_SYS + "Setting.json";

		Setting setting;

		public MainWindow()
		{
			InitializeComponent();

			Title = "VMS v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

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
					RepoUrl = @"http://admin:admin@192.168.120.129:2507/r/Test.git",
					LoaclRepoPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VMS"
				};
				File.WriteAllText(FILE_SETTING, js.Serialize(setting));
			}

			//foreach(var item in users)
			//{
			//	UserList.Items.Add(item.Key);
			//}

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
				catch(Exception e)
				{
					MessageBox.Show(e.Message, "更新失败!");
				}

				foreach(var branch in repo.Branches.Where(p => p.IsRemote))
				{
					var name = branch.FriendlyName.Split('/').Last();
					if(System.Version.TryParse(name, out System.Version version))
					{
						var panel = new DockPanel();
						var text = new TextBlock { Text = name };
						var open = new Button { Content = "打开" };
						var item = new ListBoxItem() { Content = panel,  HorizontalContentAlignment= HorizontalAlignment.Stretch };

						var sha = branch.Tip.Sha;

						//var b = Commands.Checkout(repo, sha);
						open.Click += delegate
						{
							Operate.Checkout(setting.LoaclRepoPath, name);
						};
						DockPanel.SetDock(open, Dock.Right);
						panel.Children.Add(open);
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
		}




		private void Button_Click(object sender, RoutedEventArgs e)
		{
			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(entries.IsDirty)
				{
					List<string> versions = new List<string>();
					string[] verID = { "[assembly: AssemblyVersion(\"", "[assembly: AssemblyFileVersion(\"" };
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
									if(!versions.Contains(path))
									{
										versions.Add(path);
									}
								}
							}
							break;
						default:
							break;
						}
					}

					//每次上传自动升级版本号
					foreach(var item in versions)
					{
						var file = setting.LoaclRepoPath + "/" + item + "/Properties/AssemblyInfo.cs";
						if(File.Exists(file) && !File.GetAttributes(file).HasFlag(FileAttributes.ReadOnly))
						{
							var lines = File.ReadAllLines(file, Encoding.UTF8);
							for(int i = 0; i < lines.Length; i++)
							{
								foreach(var id in verID)
								{
									if(lines[i].IndexOf(id) == 0)
									{
										var strVersion = lines[i].Substring(id.Length).Split(new char[] { '\"' })[0];
										if(System.Version.TryParse(strVersion, out System.Version version))
										{
											var newVersion = (new System.Version(version.Major, version.Minor, version.Build, version.Revision + 1)).ToString();
											lines[i] = lines[i].Replace(strVersion, newVersion);
										}
										break;
									}
								}
							}
							File.WriteAllLines(file, lines, Encoding.UTF8);
							repo.Index.Add(item + "/Properties/AssemblyInfo.cs");
						}
					}

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


		private void SetClick(object sender, RoutedEventArgs e)
		{
			using(var repo = new Repository(setting.LoaclRepoPath))
			{
				var entries = repo.RetrieveStatus();
				if(entries.IsDirty)
				{
					Commands.Checkout(repo, repo.Head.Tip, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
					//foreach(var item in entries)
					//{
					//	switch(item.State)
					//	{
					//	case FileStatus.NewInWorkdir:
					//	case FileStatus.ModifiedInWorkdir:
					//	case FileStatus.DeletedFromWorkdir:
					//	case FileStatus.TypeChangeInWorkdir:
					//	case FileStatus.RenamedInWorkdir:
					//		GitList.Items.Add(item.FilePath + " - " + item.State.ToString());
					//		repo.Index.Add(item.FilePath);
					//		break;
					//	default:
					//		break;
					//	}
					//}
					//repo.Index.Write();

					//var signa = new Signature(Environment.MachineName, repo.Head.Tip.Author.Email, DateTime.Now);
					//repo.Commit("自动提交", signa, signa);
					//repo.Network.Push(repo.Head);
				}
			}
		}
	}
}
