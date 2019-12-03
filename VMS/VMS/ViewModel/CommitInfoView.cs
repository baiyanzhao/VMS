using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LibGit2Sharp;

namespace VMS.ViewModel
{
	/// <summary>
	/// Git版本差异信息
	/// </summary>
	public class CommitInfoView
	{
		public string FilePath { get; set; }
		public FileStatus FileStatus { get; set; }
		public string State { get => FileStatus.ToString(); }
		public int FileSize
		{
			get
			{
				var info = new FileInfo(Global.Settings.LoaclRepoPath + FilePath);
				return info.Exists ? Convert.ToInt32(Math.Ceiling(info.Length / 1024.0)) : 0;
			}
		}
		public string Ext => Path.GetExtension(FilePath);

		public ICommand Diff { get; } = new DelegateCommand((parameter) =>
		{
			using var repo = new Repository(Global.Settings.LoaclRepoPath);
			var info = parameter as CommitInfoView;
			var blob = repo.Head.Tip?.Tree?[info.FilePath]?.Target as Blob;
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
				Process.Start(Global.Settings.CompareToolPath, " \"" + filePath + "\" \"" + Global.Settings.LoaclRepoPath + info.FilePath + "\"" + " /lro");
			}
			catch(Exception x)
			{
				MessageBox.Show(x.Message);
			}
		});
	}
}
