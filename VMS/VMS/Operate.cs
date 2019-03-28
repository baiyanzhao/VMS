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
		/// 更新并打开指定版本的工程
		/// </summary>
		/// <param name="repoPath"></param>
		/// <param name="name"></param>
		public static void Checkout(string repoPath, string name)
		{
			using(var repo = new Repository(repoPath))
			{
				var entries = repo.RetrieveStatus();
				if(entries.IsDirty)
				{
					if(MessageBox.Show("目录中有文件尚未上传,切换分支将导致所有更改被还原.", "是否继续!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
						return;
				}

				Commands.Fetch(repo, "origin", new string[] { "refs/heads/" + name + ":refs/heads/" + name }, null, null);
				Commands.Checkout(repo, name, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

				var prj = Directory.GetFiles(repoPath, "*.sln", SearchOption.AllDirectories);
				if(prj.Length > 0)
				{
					Process.Start(Path.GetDirectoryName( prj[0]));
				}
			}
		}

		static List<string> AssemblyFiles(List<string> files)
		{
			return null;
		}
	}
}
