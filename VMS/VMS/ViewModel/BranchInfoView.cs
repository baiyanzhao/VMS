using LibGit2Sharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VMS.Model;
using VMS.View;

namespace VMS.ViewModel
{
	/// <summary>
	/// Git分支信息管理
	/// </summary>
	class BranchInfoView : ObservableCollection<BranchInfo>
	{
		public ICommand AddCmd { get; }
		public ICommand LogCmd { get; }
		public ICommand ArchiveCmd { get; }
		public ICommand CheckoutCmd { get; }
		public string HeadName { get; set; }

		public BranchInfoView()
		{
			AddCmd = new DelegateCommand((parameter) =>
			{
				if(parameter is BranchInfo info)
				{
					using(var repo = new Repository(Global.Setting.LoaclRepoPath))
					{
						var entries = repo.RetrieveStatus();
						if(entries.IsDirty)
						{
							MessageBox.Show("当前版本已修改,请提交或撤销更改后重试!", "版本冲突");
							return;
						}

						//创建新分支
						var build = repo.Branches.Max((o) =>
						{
							if(o.IsRemote && System.Version.TryParse(o.FriendlyName.Split('/').Last(), out System.Version ver) && ver.Major == info.Version.Major && ver.Minor == info.Version.Minor)
								return ver.Build;
							return 0;
						}) + 1; //当前版本定制号
						var version = new System.Version(info.Version.Major, info.Version.Minor, build);
						var name = version.ToString();
						if(repo.Branches[name] != null)
						{
							Commands.Checkout(repo, info.Sha);
						}
						var branch = repo.Branches.Add(name, info.Sha, true);

						Commands.Checkout(repo, branch);
						repo.Branches.Update(branch, (s) => { s.TrackedBranch = "refs/remotes/origin/" + name; });

						//更新版本信息
						var versionInfo = Global.ReadVersionInfo(info.Sha);
						versionInfo = versionInfo ?? new VersionInfo();
						versionInfo.VersionBase = versionInfo.VersionNow;// new System.Version(versionInfo.VersionNow.ToString());
						versionInfo.VersionNow = version;
						Global.WriteVersionInfo(versionInfo);
					}
					MainWindow.Commit();
				}
			});

			LogCmd = new DelegateCommand((parameter) =>
			{
				if(parameter is BranchInfo info)
				{
					MainWindow.ShowLogWindow(info.Name, info.Version, info.Sha);
				}
			});

			ArchiveCmd = new DelegateCommand((parameter) =>
			{
				if(parameter is BranchInfo info)
				{
					using(var repo = new Repository(Global.Setting.LoaclRepoPath))
					{
						var name = Global.Setting.PackageFolder + info.Name + ".tar";
						repo.ObjectDatabase.Archive(repo.Lookup<Commit>(info.Sha), name);
						Process.Start("explorer", "/select,\"" + name + "\"");
					}
				}
			});

			CheckoutCmd = new DelegateCommand((parameter) =>
			{
				if(parameter is BranchInfo info)
				{
					if(Operate.Checkout(Global.Setting.LoaclRepoPath, info.Type == GitType.Sha ? info.Sha : info.Name, info.Type))
					{
						using(var repo = new Repository(Global.Setting.LoaclRepoPath))
						{
							var commit = repo.Head.Tip;
							info.Sha = commit.Sha;
							info.Author = commit.Author.Name;
							info.When = commit.Author.When;
							info.Message = commit.MessageShort;
						}
					}
				}
			});
		}
	}
}
