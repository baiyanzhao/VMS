using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LibGit2Sharp;

namespace VMS.ViewModel
{
	/// <summary>
	/// 当前文件状态
	/// </summary>
	public class CommitFileStatus
	{
		#region 属性
		public string FilePath { get; set; }
		public FileStatus FileStatus { get; set; }
		public string State => FileStatus.ToString();
		public int FileSize
		{
			get
			{
				var info = new FileInfo(GlobalShared.LoaclRepoPath + FilePath);
				return info.Exists ? Convert.ToInt32(Math.Ceiling(info.Length / 1024.0)) : 0;
			}
		}
		public string Ext => Path.GetExtension(FilePath);
		#endregion

		#region 命令
		public ICommand Diff { get; } = new DelegateCommand((parameter) =>
		{
			using var repo = new Repository(GlobalShared.LoaclRepoPath);
			var info = parameter as CommitFileStatus;
			var blob = repo.Head.Tip?.Tree?[info.FilePath]?.Target as Blob;
			if(info == null || blob == null)
				return;

			try
			{
				var filePath = Path.GetTempPath() + "\\vms@" + Path.GetRandomFileName() + "." + Path.GetFileName(info.FilePath);
				using(var stream = blob.GetContentStream(new FilteringOptions(".gitattributes")))
				{
					var bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);
					File.WriteAllBytes(filePath, bytes);
				}
				Process.Start(GlobalShared.Settings.CompareToolPath, " \"" + filePath + "\" \"" + GlobalShared.LoaclRepoPath + info.FilePath + "\"" + " /lro");
			}
			catch(Exception x)
			{
				MessageBox.Show(x.Message);
			}
		});
		#endregion
	}
}
