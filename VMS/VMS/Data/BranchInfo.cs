using System;
using System.Windows.Input;

namespace VMS.Data
{
	/// <summary>
	/// Git Tag信息
	/// </summary>
	class BranchInfo : CommitInfo
	{
		public ICommand Add
		{
			get
			{
				if(_add == null)
				{
					_add = new AddCommand();
				}
				return _add;
			}
		}

		public ICommand Checkout
		{
			get
			{
				if(_checkout == null)
				{
					_checkout = new CheckoutCommand();
				}
				return _checkout;
			}
		}

		private ICommand _add;
		private ICommand _checkout;
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
				if(!(parameter is CommitInfo info))
					return;

				MainWindow.Checkout(info.Version);
			}

			public void RaiseCanExecuteChanged()
			{
				CanExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
