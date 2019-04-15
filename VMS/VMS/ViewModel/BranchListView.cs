using LibGit2Sharp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using VMS.Model;

namespace VMS.ViewModel
{
	class BranchListView : ObservableCollection<BranchInfo>
	{
		public ICommand AddCmd { get; }
		public ICommand ArchiveCmd { get; }
		public ICommand CheckoutCmd { get; }
		public string HeadName { get; set; }

		public BranchListView()
		{
			AddCmd = new AddCommand();
			ArchiveCmd = new ArchiveCommand();
			CheckoutCmd = new CheckoutCommand();
		}

		class AddCommand : ICommand
		{
			public event EventHandler CanExecuteChanged;
			public bool CanExecute(object parameter) => true;
			public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

			public void Execute(object parameter)
			{
				switch(parameter)
				{
				case BranchInfo info:
//					MainWindow.CreateBranch(info.Name);
					break;

				case CollectionViewGroup group:
//					MainWindow.CheckoutTag(group.Name as string);
					break;
				default:
					break;
				}
			}
		}

		class ArchiveCommand : ICommand
		{
			public event EventHandler CanExecuteChanged;
			public bool CanExecute(object parameter) => true;
			public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

			public void Execute(object parameter)
			{
				if(parameter is BranchInfo info)
				{
					using(var repo = new Repository(Global.Setting.LoaclRepoPath))
					{
						var name = Global.Setting.PackageFolder + info.Name + ".zip";
						repo.ObjectDatabase.Archive(repo.Lookup<Commit>(info.Sha), name);
						Process.Start("explorer", "/select,\"" + name + "\"");
					}
				}
			}
		}

		class CheckoutCommand : ICommand
		{
			public event EventHandler CanExecuteChanged;
			public bool CanExecute(object parameter) => true;
			public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

			public void Execute(object parameter)
			{
				if(parameter is BranchInfo info)
				{
					if(Operate.Checkout(Global.Setting.LoaclRepoPath, info.Name, info.Type))
					{
						using(var repo = new Repository(Global.Setting.LoaclRepoPath))
						{
							var commit = repo.Head.Tip;
							info.Sha = commit.Sha;
							info.Author = commit.Author.Name;
							info.When = commit.Author.When;
							info.Message = commit.MessageShort;
						}
					}
				}
			}
		}
	}
}
