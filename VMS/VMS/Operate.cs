using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VMS
{
    static class Operate
    {
		/// <summary>
		/// 签出类型
		/// </summary>
		public enum CheckoutType
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
		public static bool Checkout(string repoPath, string mark, CheckoutType type = CheckoutType.Branch)
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
				case CheckoutType.Branch:
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

				case CheckoutType.Tag:
					committishOrBranchSpec = repo.Tags.FirstOrDefault(s => s.FriendlyName.Equals(mark))?.Target.Sha;
					break;

				default:
					committishOrBranchSpec = mark;
					break;
				}

				try
				{
					Commands.Checkout(repo, committishOrBranchSpec, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
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
