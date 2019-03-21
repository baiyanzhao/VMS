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
		const string DIR_DATA = DIR_WORK + @"Data\";

		const string FILE_UI = DIR_WORK + @"UI.dll";
		const string FILE_ITEM = DIR_WORK + @"Item.dll";
		const string FILE_DEVICE = DIR_WORK + @"Device.dll";

		const string FILE_DEMO = DIR_DOC + "Demo.xml";
		const string FILE_USER = DIR_SYS + "User.json";
		const string FILE_PRESET = DIR_SYS + "Preset";
		const string FILE_OFFSET = DIR_SYS + "Offset.xml";
		const string FILE_GENERAL = DIR_SYS + "General.xml";
		const string FILE_PLUG = DIR_SYS + "Plug.json";

		Repository repo;
		public MainWindow()
		{
			InitializeComponent();

			//var js = new JavaScriptSerializer();
			//Dictionary<string, int> users = null;
			//try
			//{
			//	users = js.Deserialize<Dictionary<string, int>>(File.ReadAllText(FILE_USER));
			//}
			//catch(Exception)
			//{ }

			//if(users == null)
			//{
			//	users = new Dictionary<string, int>
			//	{
			//		{ "User", 0 }
			//	};
			//}
			//File.WriteAllText(FILE_USER, js.Serialize(users));

			//foreach(var item in users)
			//{
			//	UserList.Items.Add(item.Key);
			//}

			const string LoaclPath = @"E:\XX";
			if(!Directory.Exists(LoaclPath))
			{
				Repository.Clone(@"http://admin:admin@192.168.120.129:2507/r/Test.git", LoaclPath);
			}

			repo = new Repository(LoaclPath);
			try
			{
				Commands.Pull(repo, repo.Head.Tip.Author, new PullOptions());
			}
			catch(Exception)
			{

				MessageBox.Show("请确认服务器连接正常.", "更新失败!");
			}

			foreach(var item in repo.Branches.Where(p => p.IsRemote))
			{
				GitList.Items.Add(item.FriendlyName);
			}

			//foreach(var item in repo.Branches.Where(p => !p.IsRemote))
			//{
			//	Commands.Checkout(repo, item);
			//	GitList.Items.Add(repo.Head.FriendlyName);
			//}

			foreach(var tag in repo.Tags)
			{
				UserList.Items.Add(tag.FriendlyName);
//				var ver = new System.Version(tag.FriendlyName);

			}

			//repo.Tags.Add(;

			//repo.Branches.Add

			//repo.Network.Push();
			//GitList.Items.Add("Commit:");
			//GitList.Items.Add(repo.Head.FriendlyName);
			//var cmt = repo.Head.Tip;
			//GitList.Items.Add(cmt.Committer.Name);


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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var entries = repo.RetrieveStatus();
			if(entries.IsDirty)
			{
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
						break;
					default:
						break;
					}
				}
				repo.Index.Write();

				var sig = new Signature(Environment.MachineName, repo.Head.Tip.Author.Email, DateTime.Now);
				repo.Commit("自动提交", sig, sig);
				repo.Network.Push(repo.Head);
			}
		}

		void CreateBranch(Tag tag)
		{
			var name = tag.FriendlyName + "-Auto";
			var commit = repo.Lookup<Commit>(tag.Target.Id);
			var bc = repo.Branches.Add(name, commit, true);
			repo.Branches.Update(bc, (s) => { s.TrackedBranch = "refs/remotes/origin/" + name; });
			repo.Network.Push(bc);
		}
	}
}
