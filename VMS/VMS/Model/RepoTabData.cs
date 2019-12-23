using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LibGit2Sharp;

namespace VMS.Model
{
	/// <summary>
	/// 仓库Tab控件数据
	/// </summary>
	public class RepoTabData : NotifyProperty
	{
		private RepoInfo _currentRepo;

		/// <summary>
		/// 当前仓库
		/// </summary>
		public RepoInfo CurrentRepo { get => _currentRepo; set => SetProperty(ref _currentRepo, value); }

		/// <summary>
		/// 仓库列表
		/// </summary>
		public ObservableCollection<RepoInfo> RepoList { get; } = new ObservableCollection<RepoInfo>();

		public void Update(IEnumerable<string> paths)
		{
			RepoList.Clear();
			foreach(var path in paths)
			{
				RepoList.Add(new RepoInfo(path));
				CurrentRepo ??= RepoList.FirstOrDefault();
			}
		}
	}

	/// <summary>
	/// 仓库信息
	/// </summary>
	public class RepoInfo : NotifyProperty
	{
		private string _name;
		public string FileName { get; }
		public string Name { get => _name; private set => SetProperty(ref _name, value); }
		public string LocalRepoPath { get; }
		public ObservableCollection<BranchInfo> BranchInfos { get; }

		public RepoInfo(string path)
		{
			LocalRepoPath = path;
			BranchInfos = new ObservableCollection<BranchInfo>();
			FileName = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
			Name = FileName;
			Update();
		}

		/// <summary>
		/// 更新Git数据
		/// </summary>
		public void Update()
		{
			using var repo = new Repository(LocalRepoPath);
			foreach(var tag in repo.Tags)
			{
				if(!(tag.Target is Commit commit))
					continue;

				var name = tag.FriendlyName;
				if(!System.Version.TryParse(name, out var version))
					continue;

				if(!BranchInfos.Any(info => info.Name == name))
				{
					BranchInfos.Add(new BranchInfo { Type = Git.Type.Tag, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
				}
			}

			foreach(var branch in repo.Branches.Where(p => p.IsRemote))
			{
				var commit = branch.Tip;
				var name = branch.FriendlyName.Split('/').Last();
				if(commit == null || !System.Version.TryParse(name, out var version))
					continue;

				var info = BranchInfos.FirstOrDefault(info => info.Name == name);
				if(info == null)
				{
					BranchInfos.Add(new BranchInfo { Type = Git.Type.Branch, Name = name, Sha = commit.Sha, Version = version, Author = commit.Author.Name, When = commit.Author.When, Message = commit.MessageShort });
				}
				else
				{
					info.Sha = commit.Sha;
					info.When = commit.Author.When;
					info.Author = commit.Author.Name;
					info.Message = commit.MessageShort;
				}
			}

			Name = FileName + (repo.Head.IsTracking ? "[" + repo.Head.FriendlyName + "]" : repo.Tags.FirstOrDefault(s => s.Target.Id.Equals(repo.Head.Tip.Id))?.FriendlyName);
		}
	}

	public class NotifyProperty : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			storage = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
