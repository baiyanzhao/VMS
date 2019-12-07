using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Windows.Input;
using LibGit2Sharp;
using VMS.View;

namespace VMS.Model
{
	/// <summary>
	/// Git 分支信息
	/// </summary>
	public class BranchInfo : INotifyPropertyChanged
	{
		#region 属性
		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged<TProperty>(Expression<Func<INotifyPropertyChanged, TProperty>> exp)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs((exp.Body as MemberExpression)?.Member.Name));
		}

		/// <summary>
		/// 名称
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 类型
		/// </summary>
		public GitType Type { get; set; }

		/// <summary>
		/// 版本
		/// </summary>
		public System.Version Version { get; set; }

		/// <summary>
		/// Git提交的Sha
		/// </summary>
		public string Sha
		{
			get => _sha;
			set
			{
				_sha = value;
				OnPropertyChanged(p => Sha);
			}
		}

		/// <summary>
		/// 提交者
		/// </summary>
		public string Author
		{
			get => _author;
			set
			{
				_author = value;
				OnPropertyChanged(p => Author);
			}
		}

		/// <summary>
		/// 提交信息
		/// </summary>
		public string Message
		{
			get => _message;
			set
			{
				_message = value;
				OnPropertyChanged(p => Message);
			}
		}

		/// <summary>
		/// 提交时间
		/// </summary>
		public DateTimeOffset When
		{
			get => _when;
			set
			{
				_when = value;
				OnPropertyChanged(p => When);
			}
		}
		#endregion

		#region 命令
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
				using var repo = new Repository(Global.Settings.LoaclRepoPath);
				var cmt = repo.Lookup<Commit>(info.Sha);
				var version = Global.ReadVersionInfo(cmt)?.VersionNow?.ToString();
				var path = Global.Settings.PackageFolder + (version?? info.Name) + info.Author + "\\";
				WriteFile(path, cmt.Tree);
				Process.Start("explorer", "\"" + path + "\"");

				static void WriteFile(string path, Tree tree)
				{
					if(tree == null)
						return;

					Directory.CreateDirectory(path);
					foreach(var item in tree)
					{
						switch(item.TargetType)
						{
						case TreeEntryTargetType.Blob:
							{
								using var stream = (item.Target as Blob).GetContentStream();
								var bytes = new byte[stream.Length];
								stream.Read(bytes, 0, bytes.Length);
								File.WriteAllBytes(path + item.Name, bytes);
							}
							break;
						case TreeEntryTargetType.Tree:
							WriteFile(path + item.Name + "/", item.Target as Tree);
							break;
						case TreeEntryTargetType.GitLink:
							break;
						default:
							break;
						}
					}
				}
			}
		});

		/// <summary>
		/// 检出
		/// </summary>
		public ICommand CheckoutCmd { get; } = new DelegateCommand((parameter) =>
		{
			if(parameter is BranchInfo info)
			{
				if(Global.Git.Checkout(Global.Settings.LoaclRepoPath, info.Type == GitType.Sha ? info.Sha : info.Name, info.Type))
				{
					using var repo = new Repository(Global.Settings.LoaclRepoPath);
					var commit = repo.Head.Tip;
					info.Sha = commit.Sha;
					info.Author = commit.Author.Name;
					info.When = commit.Author.When;
					info.Message = commit.MessageShort;
					MainWindow.UpdateTitle();
				}
			}
		});
		#endregion

		#region 字段
		private string _sha;
		private string _author;
		private string _message;
		private DateTimeOffset _when;
		#endregion
	}
}
