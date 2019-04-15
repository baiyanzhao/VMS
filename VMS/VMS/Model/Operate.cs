using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace VMS
{
	public static class Operate
    {
		/// <summary>
		/// 签出类型
		/// </summary>
		public enum GitType
		{
			Branch,
			Sha,
			Tag,
		}

		/// <summary>
		/// 更新并签出指定版本的工程
		/// </summary>
		/// <param name="repoPath">Git库路径</param>
		/// <param name="mark">签出标识字符串,由<paramref name="type"/>决定类型 </param>
		/// <param name="type">签出类型</param>
		public static bool Checkout(string repoPath, string mark, GitType type)
		{
			using(var repo = new Repository(repoPath))
			{
				var entries = repo.RetrieveStatus();
				if(entries.IsDirty)
				{
					if(MessageBox.Show("目录中有文件尚未上传,切换分支将导致所有更改被还原.", "是否继续!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
						return false;
				}

				string committishOrBranchSpec;
				switch(type)
				{
				case GitType.Branch:
					committishOrBranchSpec = mark;
					try
					{
						Commands.Fetch(repo, "origin", new string[] { "refs/heads/" + committishOrBranchSpec + ":refs/heads/" + committishOrBranchSpec }, null, null);
					}
					catch(Exception x)
					{
						if(MessageBox.Show(x.Message + "\n是否继续?", "数据更新失败!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
							return false;
					}
					break;

				case GitType.Tag:
					committishOrBranchSpec = repo.Tags.FirstOrDefault(s => s.FriendlyName.Equals(mark))?.Target.Sha;
					break;

				default:
					committishOrBranchSpec = mark;
					break;
				}

				try
				{
					Commands.Checkout(repo, committishOrBranchSpec, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
					if(type == GitType.Branch && !repo.Head.IsTracking)
					{
						repo.Branches.Update(repo.Head, (s) => { s.TrackedBranch = "refs/remotes/origin/" + repo.Head.FriendlyName; });
					}
				}
				catch(Exception x)
				{
					MessageBox.Show(x.Message, "切换版本库错误!");
				}
			}
			return true;
		}

		static List<string> AssemblyFiles(List<string> files)
		{
			return null;
		}
	}
}
