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
				var info = new FileInfo(GlobalShared.LocalRepoPath + FilePath);
				return info.Exists ? Convert.ToInt32(Math.Ceiling(info.Length / 1024.0)) : 0;
			}
		}
		public string Ext => Path.GetExtension(FilePath);
		#endregion

		#region 命令
		/// <summary>
		/// 对比差异
		/// </summary>
		public ICommand Diff { get; } = new DelegateCommand((parameter) =>
		{
			using var repo = new Repository(GlobalShared.LocalRepoPath);
			var info = parameter as CommitFileStatus;
			var blob = repo.Head.Tip?.Tree?[info.FilePath]?.Target as Blob;
			if(info == null)
				return;

			try
			{
				var filePath = Path.GetTempPath() + "\\vms@" + Path.GetRandomFileName() + "#" + info.FilePath.Replace('/', '.');
				if(blob != null)
				{
					using var stream = blob.GetContentStream(new FilteringOptions(info.FilePath));
					var bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);
					File.WriteAllBytes(filePath, bytes);
				}

				Process.Start(GlobalShared.Settings.CompareToolPath, " \"" + filePath + "\" \"" + GlobalShared.LocalRepoPath + info.FilePath + "\"" + " /lro");
			}
			catch(Exception x)
			{
				Serilog.Log.Error(x, "CommitFileStatus");
				MessageBox.Show(x.Message);
			}
		});

		/// <summary>
		/// 撤销更改
		/// </summary>
		public ICommand Revoke { get; } = new DelegateCommand((parameter) =>
		{
			using var repo = new Repository(GlobalShared.LocalRepoPath);
			var info = parameter as CommitFileStatus;
			var blob = repo.Head.Tip?.Tree?[info.FilePath]?.Target as Blob;
			if(info == null)
				return;

			if(MessageBox.Show("确实要撤销此文件的修改吗?\n此操作不可恢复!", "还原修改", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
				return;

			try
			{
				File.Delete(GlobalShared.LocalRepoPath + info.FilePath);
				if(blob != null)
				{
					using var stream = blob.GetContentStream(new FilteringOptions(info.FilePath));
					var bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);
					File.WriteAllBytes(GlobalShared.LocalRepoPath + info.FilePath, bytes);
				}
			}
			catch(Exception x)
			{
				Serilog.Log.Error(x, "Revoke");
				MessageBox.Show(x.Message);
			}
		});
		#endregion
	}
}
