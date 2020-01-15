using System;
using System.Windows.Input;

namespace VMS
{
	internal class DelegateCommand : ICommand
	{
		private readonly Action<object> execute;
		private readonly Predicate<object> canExecute;
		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<object> _execute, Predicate<object> _canexecute = null)
		{
			execute = _execute;
			canExecute = _canexecute;
		}

		public void Execute(object parameter) => execute?.Invoke(parameter);
		public bool CanExecute(object parameter) => canExecute == null ? true : canExecute(parameter);
		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}
