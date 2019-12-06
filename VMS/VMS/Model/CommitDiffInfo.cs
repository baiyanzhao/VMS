using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LibGit2Sharp;

namespace VMS.Model
{
	/// <summary>
	/// 已提交版本的更改信息
	/// </summary>
	internal class CommitDiffInfo
	{
		#region 方法
		public CommitDiffInfo(TreeEntryChanges tree) => Tree = tree;

		/// <summary>
		/// 根据ID生成文件
		/// </summary>
		/// <param name="blob"></param>
		/// <returns></returns>
		private static string CreateFile(ObjectId id)
		{
			using var repo = new Repository(Global.Settings.LoaclRepoPath);
			var blob = repo.Lookup<Blob>(id);
			var filePath = Path.GetTempFileName();
			if(blob != null)
			{
				using var stream = blob.GetContentStream();
				var bytes = new byte[stream.Length];
				stream.Read(bytes, 0, bytes.Length);
				File.WriteAllBytes(filePath, bytes);
			}
			return filePath;
		}
		#endregion

		#region 属性
		private TreeEntryChanges Tree { get; set; }
		public string FilePath => Tree.Path;
		public string State => Tree.Status.ToString();
		public string Ext => Path.GetExtension(FilePath);
		#endregion

		#region 命令
		public ICommand Diff { get; } = new DelegateCommand((parameter) =>
		{
			var info = parameter as CommitDiffInfo;

			try
			{
				Process.Start(Global.Settings.CompareToolPath, " \"" + CreateFile(info.Tree.OldOid) + "\" \"" + CreateFile(info.Tree.Oid) + "\"" + " /ro");
			}
			catch(Exception x)
			{
				MessageBox.Show(x.Message, "对比文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		});
		#endregion
	}
}
