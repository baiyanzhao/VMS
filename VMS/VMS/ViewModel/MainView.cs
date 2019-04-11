using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.Data;

namespace VMS.ViewModel
{
	class MainView
	{
		//public ObservableCollection<BranchInfo> BranchInfos { get; set; }

		//public MainView()
		//{
		//	BranchInfos = new ObservableCollection<BranchInfo>();
		//	using(var repo = new Repository(Global.Setting.LoaclRepoPath))
		//	{
		//		//更新分支列表
		//		BranchInfos.Clear();
		//		foreach(var branch in repo.Branches.Where(p => p.IsRemote))
		//		{
		//			var commit = branch.Tip;
		//			var name = branch.FriendlyName.Split('/').Last();
		//			if(commit == null || !System.Version.TryParse(name, out System.Version version))
		//				continue;

		//			BranchInfos.Add(new BranchInfo { Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
		//		}
		//	}
		//}
	}
}