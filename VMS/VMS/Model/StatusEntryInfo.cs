using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace VMS.Model
{
	/// <summary>
	/// Git版本差异信息
	/// </summary>
	class StatusEntryInfo
	{
		public string FilePath { get; set; }
		public string State { get; set; }

		private ICommand _diff;
		public ICommand Diff
		{
			get
			{
				_diff = _diff ?? new DiffCommand();
				return _diff;
			}
		}

		class DiffCommand : ICommand
		{
			public event EventHandler CanExecuteChanged;
			public bool CanExecute(object parameter) => true;
			public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

			public void Execute(object parameter)
			{
				using(var repo = new Repository(Global.Setting.LoaclRepoPath))
				{
					var info = parameter as StatusEntryInfo;
					var tree = repo.Index.WriteToTree();
					var blob = tree[info.FilePath]?.Target as Blob;
					if(info == null || blob == null)
						return;

					try
					{
						var filePath = Path.GetTempFileName();
						File.WriteAllText(filePath, blob.GetContentText());
						File.SetAttributes(filePath, FileAttributes.ReadOnly | FileAttributes.Temporary);
						Process.Start(Global.Setting.CompareToolPath, " \"" + filePath + "\" \"" + Global.Setting.LoaclRepoPath + info.FilePath + "\"");
					}
					catch(Exception x)
					{
						MessageBox.Show(x.Message);
					}
				}
			}
		}
	}
}
