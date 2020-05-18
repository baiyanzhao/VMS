using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using LibGit2Sharp;
using VMS.View;
using VMS.ViewModel;

namespace VMS
{
	public static class Git
	{
		#region 属性
		/// <summary>
		/// 文件状态字典
		/// </summary>
		static private readonly Dictionary<string, FileStatus> STATUS_FLAG = new Dictionary<string, FileStatus>()
		{
			{ "?? ",  LibGit2Sharp.FileStatus.NewInWorkdir},
			{ " M ",  LibGit2Sharp.FileStatus.ModifiedInWorkdir },
			{ " D ",  LibGit2Sharp.FileStatus.DeletedFromWorkdir },
			{ " C ",  LibGit2Sharp.FileStatus.TypeChangeInWorkdir },
			{ " R ",  LibGit2Sharp.FileStatus.RenamedInWorkdir },
			{ "A  ",  LibGit2Sharp.FileStatus.NewInIndex },
			{ "M  ",  LibGit2Sharp.FileStatus.ModifiedInIndex },
			{ "D  ",  LibGit2Sharp.FileStatus.DeletedFromIndex },
			{ "R  ",  LibGit2Sharp.FileStatus.RenamedInIndex },
			{ "C  ",  LibGit2Sharp.FileStatus.TypeChangeInIndex },
		};
		#endregion

		#region 方法
		/// <summary>
		/// 同步仓库
		/// </summary>
		public static void Sync(string repoPath)
		{
			/// 创建仓库
			if(Directory.GetFiles(repoPath).Length == 0 && Directory.GetDirectories(repoPath).Length == 0)
			{
				string url = null;
				Application.Current.Dispatcher.Invoke(delegate
				{
					var window = new InputWindow
					{
						ShowInTaskbar = false,
						Title = "请输入仓库URL: " + repoPath,
						Owner = Application.Current.MainWindow.IsLoaded ? Application.Current.MainWindow : null
					};

					var box = new TextBox { Text = @"http://user:ainuo@192.168.1.49:2507/r/MT.git", Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center, Background = null };
					window.InputGrid.Children.Add(box);
					window.DefaultButton.IsCancel = false;
					window.DefaultButton.Click += (s, e) =>
					{
						box.Text = box.Text.Trim();
						if(!box.Text.EndsWith(".git"))
						{
							MessageBox.Show("当前仓库地址不可用,可能的原因有:\n1. 含有非法字符\n2. 未以[.git]结尾.", "无效的仓库地址");
							return;
						}
						window.DialogResult = true;
					};
					window.ShowDialog();
					url = box.Text;
				});

				Cmd(null, "clone --verbose --progress " + url + " " + repoPath);

				/// 运行初始化批处理文件
				var cmdFile = repoPath + "/GitClone.bat";
				if(File.Exists(cmdFile))
				{
					Process.Start(new ProcessStartInfo { FileName = cmdFile, WorkingDirectory = repoPath, CreateNoWindow = true, UseShellExecute = false });
				}
			}

			/// 同步仓库
			Cmd(repoPath, "fetch --progress");
			var (Ahead, Behind, Dirty) = RepoStatus(repoPath);
			if(Behind > 0) //拉取上游分支
			{
				Cmd(repoPath, "stash clear");
				Cmd(repoPath, "stash --include-untracked");
				Cmd(repoPath, "merge --verbose --progress -srecursive -Xours");
				Cmd(repoPath, "stash pop");
			}

			if(Ahead > 0) //推送未上传的提交
			{
				Cmd(repoPath, "push --verbose --progress");
			}
		}

		/// <summary>
		/// 提交并推送
		/// </summary>
		/// <param name="repo">仓库</param>
		/// <param name="message">信息</param>
		public static bool Commit(Window owner, string repoPath, string message) => ProgressWindow.Show(owner, delegate
		{
			Cmd(repoPath, "add . --verbose");
			Cmd(repoPath, "commit --author " + GlobalShared.Settings.User + "<" + Environment.MachineName + "> -m\"" + message + "\"");
			Cmd(repoPath, "push --verbose --progress");
			Serilog.Log.Verbose("Commit {repoPath} {message}", repoPath, message);
		});

		/// <summary>
		/// 发布标准版
		/// </summary>
		/// <param name="repo">仓库</param>
		/// <param name="message">信息</param>
		/// <param name="version">新版本号</param>
		public static void Publish(Window owner, Repository repo, string message, string version) => ProgressWindow.Show(owner, delegate
		{
			/// 升级版本并提交内测版
			Cmd(repo.Info.WorkingDirectory, "add . --verbose");
			Cmd(repo.Info.WorkingDirectory, "commit --author " + "Sys<" + Environment.MachineName + "> -m\"" + version + "\"");

			/// 生成标准版提交,并设置标签
			var sign = new Signature(GlobalShared.Settings.User, Environment.MachineName, DateTime.Now);
			var parent = repo.Tags.OrderByDescending((o) =>
			{
				if(System.Version.TryParse(o.FriendlyName, out var ver))
					return ver;
				return null;
			}).FirstOrDefault()?.Target as Commit;  //获取标准版最新版本
			var cmt = repo.ObjectDatabase.CreateCommit(sign, sign, message, repo.Head.Tip.Tree, parent == null ? new Commit[] { } : new Commit[] { parent }, false);
			var tag = repo.ApplyTag(version, cmt.Sha);

			/// 内测版合并同步
			sign = new Signature("Merge", Environment.MachineName, DateTime.Now);
			repo.Merge(cmt, sign, new MergeOptions { CommitOnSuccess = true, FileConflictStrategy = CheckoutFileConflictStrategy.Theirs, MergeFileFavor = MergeFileFavor.Theirs });

			/// 上传更改
			Cmd(repo.Info.WorkingDirectory, "push origin refs/* --verbose --progress");
			Serilog.Log.Verbose("Publish {version} {message}", version, message);
		});

		/// <summary>
		/// 文件更改状态
		/// </summary>
		/// <param name="repoPath">仓库目录</param>
		/// <returns>文件更改列表</returns>
		public static ObservableCollection<CommitFileStatus> FileStatus(string repoPath)
		{
			var status = new ObservableCollection<CommitFileStatus>();
			Cmd(repoPath, "status --porcelain -z -uall", new DataReceivedEventHandler((s, e) =>
			{
				if(string.IsNullOrWhiteSpace(e.Data))
					return;

				foreach(var line in e.Data.Split('\0'))
				{
					foreach(var pair in STATUS_FLAG)
					{
						if(line.StartsWith(pair.Key))
						{
							status.Add(new CommitFileStatus { FileStatus = pair.Value, FilePath = line.Remove(0, pair.Key.Length) });
						}
					}
				}
			}));
			return status;
		}

		/// <summary>
		/// 仓库状态
		/// </summary>
		/// <param name="repoPath">仓库目录</param>
		/// <returns>状态</returns>
		public static (int Ahead, int Behind, bool IsDirty) RepoStatus(string repoPath)
		{
			bool IsDirty = false;
			var status = new (string Flag, int Num)[] { ("ahead ", 0), ("behind ", 0) };
			Cmd(repoPath, "status --porcelain -b", new DataReceivedEventHandler((s, e) =>
			{
				if(string.IsNullOrWhiteSpace(e.Data))
					return;

				var line = e.Data;
				if(line.StartsWith("## "))
				{
					for(int i = 0; i < status.Length; i++)
					{
						if(!line.Contains(status[i].Flag))
							continue;

						int begin = line.IndexOf(status[i].Flag) + status[i].Flag.Length;
						int lenth = line.IndexOfAny(new char[] { ',', ']' }, begin) - begin;
						if(begin > 0 && lenth > 0)
						{
							int.TryParse(line.Substring(begin, lenth), out status[i].Num);
						}
					}
					return;
				}

				if(!IsDirty)
				{
					foreach(var pair in STATUS_FLAG)
					{
						if(line.StartsWith(pair.Key))
						{
							IsDirty = true;
							break;
						}
					}
				}
			}));
			return (status[0].Num, status[1].Num, IsDirty);
		}

		/// <summary>
		/// 更新并签出指定版本的工程
		/// </summary>
		/// <param name="repo">Git仓库</param>
		/// <param name="mark">签出标识字符串,由<paramref name="type"/>决定类型 </param>
		/// <param name="type">签出类型</param>
		public static void Checkout(Repository repo, string mark, Type type)
		{
			if(repo == null)
				return;

			var committishOrBranchSpec = type switch
			{
				Type.Branch => mark,
				Type.Tag => repo.Tags.FirstOrDefault(s => s.FriendlyName.Equals(mark))?.Target.Sha,
				_ => mark,
			};

			Cmd(repo.Info.WorkingDirectory, "checkout " + committishOrBranchSpec + " --force --progress");
			if(type == Type.Branch)
			{
				Cmd(repo.Info.WorkingDirectory, "branch --set-upstream-to=origin/" + committishOrBranchSpec);
				Sync(repo.Info.WorkingDirectory);
			}

			/// 运行批处理文件
			var cmdFile = repo.Info.WorkingDirectory + "/GitUpdate.bat";
			if(File.Exists(cmdFile))
			{
				Process.Start(new ProcessStartInfo { FileName = cmdFile, WorkingDirectory = repo.Info.WorkingDirectory, CreateNoWindow = true, UseShellExecute = false });
			}
		}

		/// <summary>
		/// Git命令行工具
		/// </summary>
		/// <param name="workDir">仓库目录</param>
		/// <param name="cmd">命令</param>
		/// <param name="dataReceivedHandler">数据回传处理方法</param>
		public static void Cmd(string workDir, string cmd, DataReceivedEventHandler dataReceivedHandler = null)
		{
			using var process = new Process();
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = cmd;
			process.StartInfo.WorkingDirectory = workDir;
			process.StartInfo.RedirectStandardInput = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

			var errors = string.Empty;
			var ErrorPrefixes = new string[] { "error:", "fatal:" };
			var dataHandler = dataReceivedHandler ?? new DataReceivedEventHandler((s, e) =>
			{
				var msg = e.Data;
				if(string.IsNullOrWhiteSpace(msg))
					return;

				if(string.IsNullOrEmpty(errors))
				{
					foreach(var prefix in ErrorPrefixes)
					{
						if(msg.StartsWith(prefix))
						{
							errors += msg;
							break;
						}
					}
				}
				else
				{
					errors += msg;
				}

				Serilog.Log.Verbose(msg);
				ProgressWindow.Update(msg);
			});

			ProgressWindow.Update("git " + cmd);
			Serilog.Log.Information("git {cmd} {workDir} ==>", cmd, workDir);
			process.OutputDataReceived += dataHandler;
			process.ErrorDataReceived += dataHandler;
			process.Start();
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
			process.WaitForExit();
			if(!string.IsNullOrEmpty(errors))
			{
				throw new Exception(errors);
			}
			Serilog.Log.Verbose("git {cmd} <==", cmd);
		}
		#endregion

		#region 类型
		/// <summary>
		/// 签出类型
		/// </summary>
		public enum Type
		{
			Branch,
			Sha,
			Tag,
		}
		#endregion
	}
}
