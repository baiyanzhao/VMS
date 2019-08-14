using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LibGit2Sharp;

namespace VMS.Model
{
	/// <summary>
	/// Git版本差异信息
	/// </summary>
	public class StatusEntryInfo
	{
		public string FilePath { get; set; }
		public FileStatus FileStatus { get; set; }
		public string State { get => FileStatus.ToString(); }
		public ICommand Diff { get; } = new DelegateCommand((parameter) =>
		{
			using(var repo = new Repository(Global.Setting.LoaclRepoPath))
			{
				var info = parameter as StatusEntryInfo;
				var blob = repo.Head.Tip.Tree?[info.FilePath]?.Target as Blob;
				if(info == null || blob == null)
					return;

				try
				{
					var filePath = Path.GetTempFileName();
					using(var stream = blob.GetContentStream())
					{
						var bytes = new byte[stream.Length];
						stream.Read(bytes, 0, bytes.Length);
						File.WriteAllBytes(filePath, bytes);
					}
					File.SetAttributes(filePath, FileAttributes.ReadOnly | FileAttributes.Temporary);
					Process.Start(Global.Setting.CompareToolPath, " \"" + filePath + "\" \"" + Global.Setting.LoaclRepoPath + info.FilePath + "\"");
				}
				catch(Exception x)
				{
					MessageBox.Show(x.Message);
				}
			}
		});
	}
}
