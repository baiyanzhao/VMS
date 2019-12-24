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
	public class CommitDiffInfo
	{
		#region 方法
		public CommitDiffInfo(TreeEntryChanges tree) => Tree = tree;
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
				Process.Start(GlobalShared.Settings.CompareToolPath, " \"" + CreateFile(info.Tree.OldOid, Path.GetFileName(info.Tree.OldPath)) + "\" \"" + CreateFile(info.Tree.Oid, Path.GetFileName(info.FilePath)) + "\"" + " /ro");
			}
			catch(Exception x)
			{
				MessageBox.Show(x.Message, "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			/// <summary>
			/// 根据ID生成文件
			/// </summary>
			/// <param name="id">Git Oid</param>
			/// <param name="fileName"></param>
			/// <returns>文件路径</returns>
			static string CreateFile(ObjectId id, string fileName)
			{
				using var repo = new Repository(GlobalShared.LoaclRepoPath);
				var blob = repo.Lookup<Blob>(id);
				var filePath = Path.GetTempPath() + "\\vms@" + Path.GetRandomFileName() + "." + fileName;
				if(blob != null)
				{
					using var stream = blob.GetContentStream(new FilteringOptions(".gitattributes"));
					var bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);
					File.WriteAllBytes(filePath, bytes);
				}
				return filePath;
			}
		});
		#endregion
	}
}
