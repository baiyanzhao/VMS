using System;
using LibGit2Sharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using VMS.Model;
using System.Linq;
using static VMS.Operate;
using System.Windows.Threading;
using System.Windows;
using System.IO;

namespace VMS.ViewModel
{
	class BranchListView : ObservableCollection<BranchInfo>
	{
		public ICommand AddCmd { get; }
		public ICommand ArchiveCmd { get; }
		public ICommand CheckoutCmd { get; }
		public string HeadName { get; set; }

		public BranchListView()
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
						var build = this.Max((o) => { return (o.Version.Major == info.Version.Major && o.Version.Minor == info.Version.Minor) ? o.Version.Build : 0; }) + 1; //当前版本定制号
						var version = new System.Version(info.Version.Major, info.Version.Minor, build);
						var name = version.ToString();
						var branch = repo.Branches[name] ?? repo.Branches.Add(name, info.Sha);

						Commands.Checkout(repo, branch);
						repo.Branches.Update(branch, (s) => { s.TrackedBranch = "refs/remotes/origin/" + name; });

						//更新版本信息
						var versionInfo = Global.ReadVersionInfo();
						versionInfo.VersionNow = version;
						versionInfo.VersionBase = info.Version;
						Global.WriteVersionInfo(versionInfo);
					}
					MainWindow.Commit();
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
					if(Operate.Checkout(Global.Setting.LoaclRepoPath, info.Name, info.Type))
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
