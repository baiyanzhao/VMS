using System;
using System.ComponentModel;
using System.Linq.Expressions;
using static VMS.Operate;

namespace VMS.Model
{
	/// <summary>
	/// Git 分支信息
	/// </summary>
	class BranchInfo : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged<TProperty>(Expression<Func<INotifyPropertyChanged, TProperty>> exp)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs((exp.Body as MemberExpression)?.Member.Name));
		}

		public string Name { get; set; }
		public GitType Type { get; set; }
		public Version Version { get; set; }

		public string Sha
		{
			get => _sha; set
			{
				_sha = value;
				OnPropertyChanged(p => Sha);
			}
		}

		public string Author
		{
			get => _author;
			set
			{
				_author = value;
				OnPropertyChanged(p => Author);
			}
		}

		public string Message
		{
			get => _message;
			set
			{
				_message = value;
				OnPropertyChanged(p => Message);
			}
		}

		public DateTimeOffset When
		{
			get => _when;
			set
			{
				_when = value;
				OnPropertyChanged(p => When);
			}
		}

		private string _sha;
		private string _author;
		private string _message;
		private DateTimeOffset _when;
	}
}
