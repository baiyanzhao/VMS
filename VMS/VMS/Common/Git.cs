using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using LibGit2Sharp;
using VMS.View;

namespace VMS
{
	public static class Git
	{
		/// <summary>
		/// 签出类型
		/// </summary>
		public enum Type
		{
			Branch,
			Sha,
			Tag,
		}

		#region 属性
		/// <summary>
		/// Git PushOptions
		/// </summary>
		public static PushOptions GitPushOptions { get; } = new PushOptions
		{
			CredentialsProvider = GetCredential,
			OnPushTransferProgress = (int current, int total, long bytes) =>
			{
				ProgressWindow.Update(string.Format("{0}/{1}, {2}kB", current, total, string.Format("{0:N}", bytes / 1024.0)));
				return true;
			},
			OnNegotiationCompletedBeforePush = (updates) =>
			{
				return true;
			},
			OnPackBuilderProgress = (stage, current, total) =>
			{
				ProgressWindow.Update(string.Format("{0} {1}/{2}", stage, current, total));
				return true;
			},
			OnPushStatusError = (err) =>
			{
				throw new Exception(err.Message);
			}
		};

		/// <summary>
		/// Git CloneOptions
		/// </summary>
		public static CloneOptions GitCloneOptions { get; } = new CloneOptions
		{
			CredentialsProvider = GetCredential,
			OnProgress = (string serverProgressOutput) =>
			{
				ProgressWindow.Update(serverProgressOutput);
				return true;
			},
			OnUpdateTips = (string referenceName, ObjectId oldId, ObjectId newId) =>
			{
				ProgressWindow.Update(referenceName);
				return true;
			},
			OnTransferProgress = (TransferProgress progress) =>
			{
				ProgressWindow.Update(string.Format("{0}/{1}, {2}kB", progress.ReceivedObjects, progress.TotalObjects, string.Format("{0:N}", progress.ReceivedBytes / 1024.0)));
				return true;
			}
		};

		/// <summary>
		/// Git FetchOptions
		/// </summary>
		public static FetchOptions GitFetchOptions { get; } = new FetchOptions
		{
			CredentialsProvider = GetCredential,
			OnProgress = (string serverProgressOutput) =>
			{
				ProgressWindow.Update(serverProgressOutput);
				return true;
			},
			OnUpdateTips = (string referenceName, ObjectId oldId, ObjectId newId) =>
			{
				ProgressWindow.Update(referenceName);
				return true;
			},
			OnTransferProgress = (TransferProgress progress) =>
			{
				ProgressWindow.Update(string.Format("{0}/{1}, {2}kB", progress.ReceivedObjects, progress.TotalObjects, string.Format("{0:N}", progress.ReceivedBytes / 1024.0)));
				return true;
			}
		};
		#endregion

		#region 方法
		/// <summary>
		/// 同步仓库
		/// </summary>
		public static void Sync(string localPath)
		{
			//创建仓库
			if(Repository.Discover(localPath) == null)
			{
				string url = null;
				Application.Current.Dispatcher.Invoke(delegate
				{
					var window = new InputWindow
					{
						ShowInTaskbar = false,
						Title = "请输入仓库URL: " + localPath,
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

				Repository.Clone(url, localPath, GitCloneOptions);

				/// 运行初始化批处理文件
				var cmdFile = localPath + "/GitClone.bat";
				if(File.Exists(cmdFile))
				{
					Process.Start(new ProcessStartInfo { FileName = cmdFile, WorkingDirectory = localPath, CreateNoWindow = true, UseShellExecute = false });
				}
			}

			//同步仓库,并推送当前分支
			using var repo = new Repository(localPath);
			Commands.Fetch(repo, "origin", Array.Empty<string>(), GitFetchOptions, null);

			//拉取当前分支
			if(repo.Head.TrackingDetails.BehindBy > 0)
			{
				Commands.Pull(repo, new Signature("Sys", Environment.MachineName, DateTime.Now), new PullOptions { FetchOptions = GitFetchOptions });
			}

			if(repo.Head.TrackingDetails.AheadBy > 0)
			{
				Cmd(repo.Info.WorkingDirectory, "push --verbose --progress");   //推送未上传的提交
			}
		}

		/// <summary>
		/// 同步Head
		/// </summary>
		/// <param name="owner">主窗体</param>
		/// <param name="repo">仓库</param>
		/// <returns></returns>
		public static bool FetchHead(Window owner, Repository repo) => ProgressWindow.Show(owner, delegate
		{
			Commands.Fetch(repo, "origin", Array.Empty<string>(), GitFetchOptions, null);
			if(repo.Head.TrackingDetails.BehindBy > 0) //以Sys名称拉取上游分支
			{
				Commands.Pull(repo, new Signature("Sys", Environment.MachineName, DateTime.Now), new PullOptions { FetchOptions = GitFetchOptions });
			}
		});

		/// <summary>
		/// 提交并推送
		/// </summary>
		/// <param name="repo">仓库</param>
		/// <param name="message">信息</param>
		public static bool Commit(Window owner, Repository repo, string message) => ProgressWindow.Show(owner, delegate
		{
			var sign = new Signature(GlobalShared.Settings.User, Environment.MachineName, DateTime.Now);
			ProgressWindow.Update("Stage...");
			Commands.Stage(repo, "*");
			repo.Commit(message, sign, sign);
			Cmd(repo.Info.WorkingDirectory, "push --verbose --progress");
			Serilog.Log.Verbose("Commit {FriendlyName} {message}", repo.Head.FriendlyName, message);
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
			var sign = new Signature("Sys", Environment.MachineName, DateTime.Now);
			ProgressWindow.Update("Stage...");
			Commands.Stage(repo, "*");
			repo.Commit(version, sign, sign);

			/// 生成标准版提交,并设置标签
			sign = new Signature(GlobalShared.Settings.User, Environment.MachineName, DateTime.Now);
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

		public static void Cmd(string workDir, string cmd)
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
			var dataHandler = new DataReceivedEventHandler((s, e) =>
			{
				var msg = e.Data;
				if(string.IsNullOrWhiteSpace(msg))
					return;

				foreach(var prefix in ErrorPrefixes)
				{
					if(msg.StartsWith(prefix))
					{
						errors += msg;
						break;
					}
				}

				Serilog.Log.Verbose(msg);
				ProgressWindow.Update(msg);
			});

			Serilog.Log.Information("git {cmd} {workDir}", cmd, workDir);
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
			Serilog.Log.Verbose("git End");
		}

		/// <summary>
		/// 更新并签出指定版本的工程
		/// </summary>
		/// <param name="repo">Git仓库</param>
		/// <param name="mark">签出标识字符串,由<paramref name="type"/>决定类型 </param>
		/// <param name="type">签出类型</param>
		public static void Checkout(Repository repo, string mark, Type type)
		{
			string committishOrBranchSpec;
			switch(type)
			{
			case Type.Branch:
				committishOrBranchSpec = mark;
				if(repo.Branches[committishOrBranchSpec]?.IsTracking == true && repo.Branches[committishOrBranchSpec]?.TrackingDetails.AheadBy > 0)
				{
					Cmd(repo.Info.WorkingDirectory, "push origin " + committishOrBranchSpec + " --verbose --progress");  //同步前先推送,防止本地更改被远程覆盖
				}
				Commands.Fetch(repo, "origin", new string[] { "refs/heads/" + committishOrBranchSpec + ":refs/heads/" + committishOrBranchSpec }, GitFetchOptions, null);
				break;

			case Type.Tag:
				committishOrBranchSpec = repo.Tags.FirstOrDefault(s => s.FriendlyName.Equals(mark))?.Target.Sha;
				break;

			default:
				committishOrBranchSpec = mark;
				break;
			}

			Commands.Checkout(repo, committishOrBranchSpec, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
			if(type == Type.Branch && !repo.Head.IsTracking)
			{
				repo.Branches.Update(repo.Head, (s) => { s.TrackedBranch = "refs/remotes/origin/" + repo.Head.FriendlyName; });
			}

			/// 运行批处理文件
			var cmdFile = repo.Info.WorkingDirectory + "/GitUpdate.bat";
			if(File.Exists(cmdFile))
			{
				Process.Start(new ProcessStartInfo { FileName = cmdFile, WorkingDirectory = repo.Info.WorkingDirectory, CreateNoWindow = true, UseShellExecute = false });
			}
		}

		private static UsernamePasswordCredentials GetCredential(string url, string usernameFromUrl, SupportedCredentialTypes types)
		{
			string user = null, password = null;
			if(GlobalShared.Settings.CredentialPairs.ContainsKey((url, usernameFromUrl)))
			{
				var (User, Password) = GlobalShared.Settings.CredentialPairs[(url, usernameFromUrl)];
				user = User;
				password = Password;
			}
			else
			{
				Application.Current.Dispatcher.Invoke(delegate
				{
					var window = new InputWindow
					{
						ShowInTaskbar = false,
						Title = "请输入仓库账号和密码:" + url,
						Owner = Application.Current.MainWindow.IsLoaded ? Application.Current.MainWindow : null
					};

					var userBox = new TextBox { Text = usernameFromUrl, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center, Background = null };
					var passwordBox = new PasswordBox { Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
					window.InputGrid.Children.Add(userBox);
					window.InputGrid.Children.Add(passwordBox);
					window.ShowDialog();
					user = userBox.Text;
					password = passwordBox.Password;
				});

				if(!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
				{
					GlobalShared.Settings.CredentialPairs.Add((url, usernameFromUrl), (user, password));
					GlobalShared.WriteSetting();
				}
			}
			return new UsernamePasswordCredentials() { Username = user, Password = password };
		}
		#endregion
	}
}