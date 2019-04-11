using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using VMS.Data;

namespace VMS.ViewModel
{
	class BranchListView : ObservableCollection<BranchInfo>
	{
		//public event PropertyChangedEventHandler PropertyChanged;
		//void OnPropertyChanged<TProperty>(Expression<Func<INotifyPropertyChanged, TProperty>> exp)
		//{
		//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs((exp.Body as MemberExpression)?.Member.Name));
		//}
		public ICommand AddCmd { get; }
		public ICommand CheckoutCmd { get; }

		public BranchListView()
		{
			AddCmd = new AddCommand();
			CheckoutCmd = new CheckoutCommand();
		}

		class AddCommand : ICommand
		{
			public event EventHandler CanExecuteChanged;
			public bool CanExecute(object parameter)
			{
				return true;
			}

			public void Execute(object parameter)
			{
			}

			public void RaiseCanExecuteChanged()
			{
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		class CheckoutCommand : ICommand
		{
			public event EventHandler CanExecuteChanged;
			public bool CanExecute(object parameter)
			{
				return true;
			}

			public void Execute(object parameter)
			{
				switch(parameter)
				{
				case CommitInfo info:
					MainWindow.CheckoutBranch(info.Name);
					break;

				case CollectionViewGroup group:
					MainWindow.CheckoutTag(group.Name as string);
					break;
				default:
					break;
				}
			}

			public void RaiseCanExecuteChanged()
			{
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
