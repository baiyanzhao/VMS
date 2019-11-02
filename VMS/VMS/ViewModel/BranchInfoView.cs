using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LibGit2Sharp;
using VMS.Model;
using VMS.View;

namespace VMS.ViewModel
{
	/// <summary>
	/// Git分支信息管理
	/// </summary>
	class BranchInfoView : ObservableCollection<BranchInfo>
	{
		/// <summary>
		/// Git当前分支名称
		/// </summary>
		public string HeadName { get; set; }

		/// <summary>
		/// 新建分支
		/// </summary>
		public ICommand AddCmd { get; } = new DelegateCommand((parameter) =>
		{
			if(parameter is BranchInfo info)
			{
				using var repo = new Repository(Global.Setting.LoaclRepoPath);
				var entries = repo.RetrieveStatus();
				if(entries.IsDirty)
				{
					MessageBox.Show("当前版本已修改,请提交或撤销更改后重试!", "版本冲突");
					return;
				}

				try
				{
					Commands.Fetch(repo, "origin", new string[0], null, null);
				}
				catch
				{
					MessageBox.Show("连接服务器失败,请检查网络连接或重启软件后重试!", "同步失败");
					return;
				}

				//创建新分支
				var build = repo.Branches.Max((o) =>
				{
					if(o.IsRemote && System.Version.TryParse(o.FriendlyName.Split('/').Last(), out var ver) && ver.Major == info.Version.Major && ver.Minor == info.Version.Minor)
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
				versionInfo.VersionBase = versionInfo.VersionNow;
				versionInfo.VersionNow = version;
				Global.WriteVersionInfo(versionInfo);

				if(MainWindow.Commit(repo) != true) //提交新分支,提交失败则删除新分支
				{
					Commands.Checkout(repo, info.Sha);
					repo.Branches.Remove(branch);
				}
			}
		});

		/// <summary>
		/// 显示历史记录
		/// </summary>
		public ICommand LogCmd { get; } = new DelegateCommand((parameter) =>
		{
			if(parameter is BranchInfo info)
			{
				MainWindow.ShowLogWindow(info.Name, info.Version, info.Sha);
			}
		});

		class ArchiveSelf : ArchiverBase
		{
			public string OutFileName { get; set; }

			protected override void AddTreeEntry(string path, TreeEntry entry, DateTimeOffset modificationTime)
			{
				string ss = path;
			}
		}

		/// <summary>
		/// 另存为归档文件
		/// </summary>
		public ICommand ArchiveCmd { get; } = new DelegateCommand((parameter) =>
		{
			if(parameter is BranchInfo info)
			{
				using var repo = new Repository(Global.Setting.LoaclRepoPath);
				var cmt = repo.Lookup<Commit>(info.Sha);
				var version = Global.ReadVersionInfo(cmt)?.VersionNow?.ToString();
				version = version == null ? info.Name : version + " " + info.Author;
				var name = Global.Setting.PackageFolder + (version ?? info.Name) + ".tar";
				repo.ObjectDatabase.Archive(cmt, /*name*/new ArchiveSelf { OutFileName = name });
				Process.Start("explorer", "/select,\"" + name + "\"");
			}
		});

		/// <summary>
		/// 检出
		/// </summary>
		public ICommand CheckoutCmd { get; } = new DelegateCommand((parameter) =>
		{
			if(parameter is BranchInfo info)
			{
				if(Operate.Checkout(Global.Setting.LoaclRepoPath, info.Type == GitType.Sha ? info.Sha : info.Name, info.Type))
				{
					using var repo = new Repository(Global.Setting.LoaclRepoPath);
					var commit = repo.Head.Tip;
					info.Sha = commit.Sha;
					info.Author = commit.Author.Name;
					info.When = commit.Author.When;
					info.Message = commit.MessageShort;
					MessageBox.Show(info.Author + "\r" + info.Message + "\r" + info.When, "检出版本: " + info.Name);
				}
			}
		});
	}
}
