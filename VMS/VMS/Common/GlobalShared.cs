using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using LibGit2Sharp;
using VMS.Model;

namespace VMS
{
	public static class GlobalShared
	{
		#region 属性
		private const string FILE_VERSION_INFO = "Version.json"; //定制信息
		private const string FILE_SETTING_LOCAL = "Setting.json"; //设置
		private static string SetFilePath => ApplicationDeployment.IsNetworkDeployed ? Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, FILE_SETTING_LOCAL) : FILE_SETTING_LOCAL;
		public static Setting Settings { get; } = GetSetting();
		public static RepoTabData RepoData { get; } = new RepoTabData();
		public static string LocalRepoPath => RepoData.CurrentRepo?.LocalRepoPath;
		#endregion

		#region 方法
		private static Setting GetSetting()
		{
			var set = ReadObject<Setting>(SetFilePath) ?? new Setting { IsAutoCommit = false, IsTipsCommit = true, IsDirectExit = false };
			set.RepoPathList ??= new List<string>();
			set.LatestMessage ??= new List<string>();
			set.PackageFolder ??= Path.GetTempPath() + @"Package\";
			set.CompareToolPath ??= @"D:\Program Files\Beyond Compare 4\BCompare.exe";
			set.MSBuildPath ??= @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe";
			return set;
		}

		public static void WriteSetting() => WriteObject(SetFilePath, Settings);

		/// <summary>
		/// 序列化
		/// </summary>
		public static bool WriteObject<T>(string path, T val) where T : class
		{
			try
			{
				File.Delete(path);
				using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
				new DataContractJsonSerializer(typeof(T)).WriteObject(stream, val);
				File.SetAttributes(path, FileAttributes.Hidden);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 反序列化
		/// </summary>
		private static T ReadObject<T>(string path) where T : class
		{
			T val = default;
			if(!File.Exists(path))
				return val;

			try
			{
				using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				val = new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e);
			}

			return val;
		}

		/// <summary>
		/// 工程版本信息
		/// </summary>
		public static VersionInfo ReadVersionInfo() => ReadObject<VersionInfo>(Path.Combine(LocalRepoPath, FILE_VERSION_INFO));

		/// <summary>
		/// 工程版本信息
		/// </summary>
		public static VersionInfo ReadVersionInfo(string sha)
		{
			try
			{
				using var repo = new Repository(LocalRepoPath);
				return ReadVersionInfo(repo.Lookup<Commit>(sha));
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e);
			}

			return null;
		}

		/// <summary>
		/// 工程版本信息
		/// </summary>
		public static VersionInfo ReadVersionInfo(Commit commit)
		{
			VersionInfo version = null;
			try
			{
				var stream = (commit?.Tree["Version.json"]?.Target as Blob)?.GetContentStream();
				version = stream == null ? null : new DataContractJsonSerializer(typeof(VersionInfo)).ReadObject(stream) as VersionInfo;
				if(version != null)
				{
					version.CommitMessage = commit.Message;
				}
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e);
			}

			return version;
		}

		/// <summary>
		/// 更新版本,并写入文件
		/// </summary>
		/// <param name="info"></param>
		public static void WriteVersionInfo(VersionInfo info) => WriteObject(Path.Combine(LocalRepoPath, FILE_VERSION_INFO), info);

		/// <summary>
		/// 获取当前提交的更改列表
		/// </summary>
		/// <param name="sha">Git Sha</param>
		/// <returns></returns>
		public static IEnumerable<LogTreeDiff> GetDiffList(string sha)
		{
			var diffInfo = new ObservableCollection<LogTreeDiff>();
			try
			{
				using var repo = new Repository(LocalRepoPath);
				var commit = repo.Lookup<Commit>(sha);
				foreach(var item in repo.Diff.Compare<TreeChanges>(commit.Parents.FirstOrDefault()?.Tree, commit.Tree))
				{
					diffInfo.Add(new LogTreeDiff(item));
				}
			}
			catch
			{ }

			return diffInfo;
		}
		#endregion
	}
}
