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
			MainWindow.CreateBranch(parameter as BranchInfo);
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
				var name = Global.Setting.PackageFolder + (version == null ? info.Name : version + " " + info.Author) + ".tar";
				repo.ObjectDatabase.Archive(cmt, name);
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
					Application.Current.MainWindow.Title = "版本管理 分支:" + info.Name + " " + info.Author;
					MessageBox.Show(info.Author + "\r" + info.Message + "\r" + info.When, "检出版本: " + info.Name);
				}
			}
		});
	}
}
