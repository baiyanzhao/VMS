using System;
using System.ComponentModel;
using System.Linq.Expressions;

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
		public Version Version { get; set; }

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

		private string _sha;
		private string _author;
		private string _message;
		private DateTimeOffset _when;
	}
}
