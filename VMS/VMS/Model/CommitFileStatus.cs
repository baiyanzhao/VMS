﻿using System;
using System.Collections.ObjectModel;
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

		/// <summary>
		/// 对比差异
		/// </summary>
		public ICommand Diff { get; }

		/// <summary>
		/// 撤销更改
		/// </summary>
		public ICommand Revoke { get; }
		#endregion

		public CommitFileStatus()
		{
			#region 命令
			Diff = new DelegateCommand((parameter) =>
			{
				using var repo = new Repository(GlobalShared.LocalRepoPath);
				var blob = repo.Head.Tip?.Tree?[FilePath]?.Target as Blob;

				try
				{
					var filePath = Path.GetTempPath() + "\\vms@" + Path.GetRandomFileName() + "#" + FilePath.Replace('/', '.');
					if(blob != null)
					{
						using var stream = blob.GetContentStream(new FilteringOptions(FilePath));
						var bytes = new byte[stream.Length];
						stream.Read(bytes, 0, bytes.Length);
						File.WriteAllBytes(filePath, bytes);
					}

					Process.Start(GlobalShared.Settings.CompareToolPath, " \"" + filePath + "\" \"" + GlobalShared.LocalRepoPath + FilePath + "\"" + " /lro");
				}
				catch(Exception x)
				{
					Serilog.Log.Error(x, "CommitFileStatus Diff");
					MessageBox.Show(x.Message);
				}
			});

			Revoke = new DelegateCommand((parameter) =>
			{

				using var repo = new Repository(GlobalShared.LocalRepoPath);
				var blob = repo.Head.Tip?.Tree?[FilePath]?.Target as Blob;
				if(MessageBox.Show("确实要撤销此文件的修改吗?\n此操作不可恢复!", "还原修改", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
					return;

				try
				{
					File.Delete(GlobalShared.LocalRepoPath + FilePath);
					if(blob != null)
					{
						using var stream = blob.GetContentStream(new FilteringOptions(FilePath));
						var bytes = new byte[stream.Length];
						stream.Read(bytes, 0, bytes.Length);
						File.WriteAllBytes(GlobalShared.LocalRepoPath + FilePath, bytes);
					}

					(parameter as ObservableCollection<CommitFileStatus>)?.Remove(this);
				}
				catch(Exception x)
				{
					Serilog.Log.Error(x, "CommitFileStatus Revoke");
					MessageBox.Show(x.Message);
				}
			});
			#endregion
		}
	}
}
