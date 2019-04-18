using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.Model;

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
		/// <param name="repo">Git仓库</param>
		/// <param name="mark">签出标识字符串,由<paramref name="type"/>决定类型 </param>
		/// <param name="type">签出类型</param>
		public static bool Checkout(Repository repo, string mark, GitType type)
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
			return true;
		}

		/// <summary>
		/// 更新并签出指定版本的工程
		/// </summary>
		/// <param name="repoPath">Git仓库路径</param>
		/// <param name="mark">签出标识字符串,由<paramref name="type"/>决定类型 </param>
		/// <param name="type">签出类型</param>
		public static bool Checkout(string repoPath, string mark, GitType type)
		{
			using(var repo = new Repository(repoPath))
			{
				return Checkout(repo, mark, type);
			}
		}

		public static bool CanCommit(Repository repo, string repoPath, out ICollection<StatusEntryInfo> status, out List<string> versionFiles)
		{
			versionFiles = new List<string>();
			status = new Collection<StatusEntryInfo>();
			var entries = repo.RetrieveStatus();
			if(!entries.IsDirty || !repo.Head.IsTracking)
				return false;

			var allProperties = Directory.GetDirectories(Global.Setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories); //版本配置文件所在目录的相对路径
			for(int i = 0; i < allProperties.Length; i++)
			{
				allProperties[i] = Path.GetDirectoryName(allProperties[i]).Substring(Global.Setting.LoaclRepoPath.Length).Replace('\\', '/');
			}

			foreach(var item in entries)
			{
				switch(item.State)
				{
				case FileStatus.NewInWorkdir:
				case FileStatus.ModifiedInWorkdir:
				case FileStatus.TypeChangeInWorkdir:
				case FileStatus.RenamedInWorkdir:
				case FileStatus.DeletedFromWorkdir:
					//status.Add(new StatusEntryInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
					foreach(var path in allProperties)
					{
						if(item.FilePath.Contains(path))
						{
							var file = path + "/Properties/AssemblyInfo.cs";
							if(!versionFiles.Contains(file))
							{
								versionFiles.Add(file);
							}
						}
					}
					break;
				default:
					break;
				}
			}
			return true;
		}

		/// <summary>
		/// 提交新版本
		/// </summary>
		/// <returns>提交成功, true: 否则,false</returns>
		public static bool Commit(Repository repo)
		{
			var entries = repo.RetrieveStatus();
			if(!entries.IsDirty || !repo.Head.IsTracking)
				return true;

			List<string> verFiles = new List<string>(); //AssemblyInfo文件的相对路径
			var allProperties = Directory.GetDirectories(Global.Setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories); //版本配置文件所在目录的相对路径
			for(int i = 0; i < allProperties.Length; i++)
			{
				allProperties[i] = Path.GetDirectoryName(allProperties[i]).Substring(Global.Setting.LoaclRepoPath.Length).Replace('\\', '/');
			}

			var infos = new ObservableCollection<StatusEntryInfo>(); //文件信息
			foreach(var item in entries)
			{
				switch(item.State)
				{
				case FileStatus.NewInWorkdir:
				case FileStatus.ModifiedInWorkdir:
				case FileStatus.TypeChangeInWorkdir:
				case FileStatus.RenamedInWorkdir:
//					repo.Index.Add(item.FilePath);
//					infos.Add(new StatusEntryInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
					foreach(var path in allProperties)
					{
						if(item.FilePath.Contains(path))
						{
							var file = path + "/Properties/AssemblyInfo.cs";
							if(!verFiles.Contains(file))
							{
								verFiles.Add(file);
							}
						}
					}
					break;

				case FileStatus.DeletedFromWorkdir:
//					repo.Index.Remove(item.FilePath);
//					infos.Add(new StatusEntryInfo() { FilePath = item.FilePath, State = item.State.ToString().Remove(1) });
					break;

				default:
					break;
				}
			}

			var owner = Application.Current.MainWindow;
			var commitWindow = new CommitWindow() { Owner = owner };
//			commitWindow.BranchName.Text = repo.Head.FriendlyName;
			commitWindow.Status.DataContext = infos;
			if(commitWindow.ShowDialog() != true)
				return false;

			//版本提交说明
			var cmtText = new StringBuilder();
			foreach(var item in verFiles)
			{
				if(AddVersion(Path.Combine(Global.Setting.LoaclRepoPath, item), out string ver))
				{
					cmtText.Append(item.Split('/')[0]);
					cmtText.Append(' ');
					cmtText.Append(ver);
					cmtText.Append('\n');

//					repo.Index.Add(item);
				}
			}
			cmtText.Append(commitWindow.Message.Text);

			//提交
			ProgressWindow.Show(owner, delegate
			{
				Commands.Stage(repo, "*");
//				repo.Index.Write();
				var sign = new Signature(Global.Setting.User, Environment.MachineName, DateTime.Now);
				repo.Commit(cmtText.ToString(), sign, sign);

				var isRetry = false;
				do
				{
					try
					{
						isRetry = false;
						repo.Network.Push(repo.Head);
					}
					catch(Exception x)
					{
						owner.Dispatcher.Invoke(delegate { isRetry = (MessageBox.Show(owner, x.Message, "推送失败,请关闭其它应用后重试!", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK); });
					}

				} while(isRetry);
			}, delegate
			{
					//var commit = repo.Head.Tip;
					//var info = _branchInfos.FirstOrDefault(p => p.Name.Equals(repo.Head.FriendlyName));
					//if(info != null)
					//{
					//	info.Sha = commit.Sha;
					//	info.Author = commit.Author.Name;
					//	info.When = commit.Author.When;
					//	info.Message = commit.MessageShort;
					//}
				});

			return true;
		}

		/// <summary>
		/// 上传时,自动升级版本号
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private static bool AddVersion(string file, out string newVersion)
		{
			newVersion = string.Empty;
			try
			{
				var lines = File.ReadAllLines(file, Encoding.UTF8);
				const string verKey = "[assembly: AssemblyFileVersion(\"";
				for(int i = 0; i < lines.Length; i++)
				{
					if(lines[i].IndexOf(verKey) == 0)
					{
						var strVersion = lines[i].Substring(verKey.Length).Split(new char[] { '\"' })[0];
						if(System.Version.TryParse(strVersion, out System.Version version))
						{
							newVersion = (new System.Version(version.Major, version.Minor, version.Build, version.Revision + 1)).ToString();
							lines[i] = lines[i].Replace(strVersion, newVersion);
						}
						break;
					}
				}
				File.WriteAllLines(file, lines, Encoding.UTF8);
				return true;
			}
			catch(Exception)
			{ }
			return false;
		}
	}
}
