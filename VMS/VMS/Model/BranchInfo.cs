using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;
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

		private void OnPropertyChanged<TProperty>(Expression<Func<INotifyPropertyChanged, TProperty>> exp) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs((exp.Body as MemberExpression)?.Member.Name));

		/// <summary>
		/// 名称
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 类型
		/// </summary>
		public Git.Type Type { get; set; }

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
			MainWindow.ArchiveCommit(parameter as BranchInfo);
		});

		/// <summary>
		/// 检出
		/// </summary>
		public ICommand CheckoutCmd { get; } = new DelegateCommand((parameter) =>
		{
			if(parameter is BranchInfo info)
			{
				ProgressWindow.Show(Application.Current.MainWindow, delegate
				{
					using var repo = new Repository(GlobalShared.LocalRepoPath);
					if(Git.Checkout(repo, info.Type == Git.Type.Sha ? info.Sha : info.Name, info.Type))
					{
						var commit = repo.Head.Tip;
						info.Sha = commit.Sha;
						info.Author = commit.Author.Name;
						info.When = commit.Author.When;
						info.Message = commit.MessageShort;
						GlobalShared.RepoData.CurrentRepo?.Update();
					}
				});
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
