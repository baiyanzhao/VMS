using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using System.Windows;
using LibGit2Sharp;
using VMS.Model;
using VMS.View;

namespace VMS
{
	/// <summary>
	/// 签出类型
	/// </summary>
	public enum GitType
	{
		Branch,
		Sha,
		Tag,
	}

	internal static class Global
	{
		private const string FILE_VERSION_INFO = "Version.json";        //定制信息
		private const string FILE_SETTING_LOCAL = "Config\\Setting.json";  //设置
		public static readonly string FILE_SETTING = ApplicationDeployment.IsNetworkDeployed ? Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, FILE_SETTING_LOCAL) : FILE_SETTING_LOCAL;
		public static Setting Setting = GetSetting();

		static Setting GetSetting()
		{
			Setting setting = null;

			//配置文件
			try
			{
				setting = new JavaScriptSerializer().Deserialize<Setting>(File.ReadAllText(FILE_SETTING));
			}
			catch(Exception)
			{ }

			setting ??= new Setting();
			setting.RepoPathList ??= new List<string>();
			setting.PackageFolder ??= Path.GetTempPath() + @"Package\";
			setting.CompareToolPath ??= @"D:\Program Files\Beyond Compare 4\BCompare.exe";
			setting.LoaclRepoPath ??= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VMS\";
			setting.MSBuildPath ??= @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe";
			return setting;
		}

		/// <summary>
		/// 程序集信息
		/// </summary>
		public class AssemblyInfo
		{
			/// <summary>
			/// 工程类型
			/// </summary>
			public ProjectType Type { get; set; }

			/// <summary>
			/// 工程文件夹的相对路径
			/// </summary>
			public string ProjectPath { get; set; }

			/// <summary>
			/// 版本配置文件的绝对路径
			/// </summary>
			public string FilePath { get; set; }

			/// <summary>
			/// 工程存在修改的文件
			/// </summary>
			public bool IsModified { get; set; }

			/// <summary>
			/// 当前版本
			/// </summary>
			public System.Version Version { get; set; }

			/// <summary>
			/// 更新当前版本,如果工程修改则递增Revision,并修改Build,同时更新相应文件
			/// </summary>
			/// <param name="versionBuild">版本定制号</param>
			public void HitVersion(int versionBuild)
			{
				//C#工程版本格式为: [assembly: AssemblyFileVersion("1.3.0.0")]
				//C工程版本格式为: static const char VERSION[] = "1.0.0.0";
				var verKey = Type == ProjectType.CSharp ? "[assembly: AssemblyFileVersion(\"" : "static const char VERSION[] = \"";
				var encoding = FileEncoding.EncodingType.GetType(FilePath);
				var lines = File.ReadAllLines(FilePath, encoding);
				for(var i = 0; i < lines.Length; i++)
				{
					if(lines[i].IndexOf(verKey) == 0)
					{
						var strVersion = lines[i].Substring(verKey.Length).Split(new char[] { '\"' })[0];
						if(System.Version.TryParse(strVersion, out var version))
						{
							if(IsModified)
							{
								var revision = version.Build == versionBuild ? version.Revision + 1 : 0;
								Version = (new System.Version(version.Major, version.Minor, versionBuild, revision));
								lines[i] = lines[i].Replace(strVersion, Version.ToString());
								File.WriteAllLines(FilePath, lines, encoding);
							}
							else
							{
								Version = version;
							}
						}
						break;
					}
				}
			}

			public enum ProjectType { C, CSharp }
		}

		/// <summary>
		/// 程序集信息
		/// </summary>
		public static IList<AssemblyInfo> GetAssemblyInfo()
		{
			var list = new List<AssemblyInfo>();

			//检索C#工程版本配置
			foreach(var item in Directory.GetDirectories(Setting.LoaclRepoPath, "Properties", SearchOption.AllDirectories))
			{
				var file = Path.Combine(item, "AssemblyInfo.cs");
				if(!File.Exists(file))
					continue;

				list.Add(new AssemblyInfo()
				{
					FilePath = file,
					Type = AssemblyInfo.ProjectType.CSharp,
					ProjectPath = Path.GetDirectoryName(item).Substring(Setting.LoaclRepoPath.Length).Replace('\\', '/')
				});
			}

			//检索C工程版本
			foreach(var item in Directory.GetDirectories(Setting.LoaclRepoPath, "Inc", SearchOption.AllDirectories))
			{
				var file = Path.Combine(item, "Version.h");
				if(!File.Exists(file))
					continue;

				list.Add(new AssemblyInfo()
				{
					FilePath = file,
					Type = AssemblyInfo.ProjectType.C,
					ProjectPath = Path.GetDirectoryName(item).Substring(Setting.LoaclRepoPath.Length).Replace('\\', '/')
				});
			}

			return list;
		}

		/// <summary>
		/// 序列化
		/// </summary>
		private static bool WriteObject<T>(string path, T val) where T : class
		{
			try
			{
				using(Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					new DataContractJsonSerializer(typeof(T)).WriteObject(stream, val);
				}
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
				using Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
		public static VersionInfo ReadVersionInfo()
		{
			return ReadObject<VersionInfo>(Path.Combine(Setting.LoaclRepoPath, FILE_VERSION_INFO));
		}

		/// <summary>
		/// 工程版本信息
		/// </summary>
		public static VersionInfo ReadVersionInfo(string sha)
		{
			try
			{
				using var repo = new Repository(Setting.LoaclRepoPath);
				return ReadVersionInfo(repo.Lookup<Commit>(sha));
			}
			catch
			{ }

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
				var obj = commit.Tree["Version.json"]?.Target as Blob;
				version = obj == null ? null : new DataContractJsonSerializer(typeof(VersionInfo)).ReadObject(obj.GetContentStream()) as VersionInfo;
				if(version != null)
				{
					version.Message = commit.Message;
				}
			}
			catch
			{ }

			return version;
		}

		/// <summary>
		/// 更新版本,并写入文件
		/// </summary>
		/// <param name="info"></param>
		public static void WriteVersionInfo(VersionInfo info)
		{
			WriteObject(Path.Combine(Setting.LoaclRepoPath, FILE_VERSION_INFO), info);
		}

		public static class Git
		{
			/// <summary>
			/// 同步仓库
			/// </summary>
			public static void Sync()
			{
				//创建仓库
				if(Repository.Discover(Setting.LoaclRepoPath) == null)
				{
					string url = null;
					Application.Current.Dispatcher.Invoke(delegate
					{
						var window = new InputWindow
						{
							Title = "请输入仓库URL"
						};
						window.InputBox.Text = @"http://admin:admin@192.168.1.49:2507/r/MT.git";
						window.ShowDialog();
						url = window.InputBox.Text;
					});

					Repository.Clone(url, Setting.LoaclRepoPath);
					using var repo = new Repository(Setting.LoaclRepoPath);
					repo.Branches.Update(repo.Branches["master"], (s) => s.TrackedBranch = null);    //取消master的上流分支,禁止用户提交此分支
				}

				//同步仓库,并推送当前分支
				using(var repo = new Repository(Setting.LoaclRepoPath))
				{
					//同步仓库
					repo.Network.Fetch(repo.Network.Remotes.First());

					//拉取当前分支
					if(repo.Head.TrackingDetails.BehindBy > 0)
					{
						repo.Network.Pull(new Signature("Sys", Environment.MachineName, DateTime.Now), new PullOptions());
					}

					//推送未上传的提交
					if(repo.Head.TrackingDetails.AheadBy > 0)
					{
						repo.Network.Push(repo.Head);
					}
				}
			}

			/// <summary>
			/// 提交并推送
			/// </summary>
			/// <param name="repo">仓库</param>
			/// <param name="message">信息</param>
			public static void Commit(Repository repo, string message, Action<string> onProgress)
			{
				repo.Stage("*");
				var sign = new Signature(Setting.User, Environment.MachineName, DateTime.Now);
				repo.Commit(message, sign, sign);
				repo.Network.Push(repo.Head, new PushOptions()
				{
					CredentialsProvider = (string url, string usernameFromUrl, SupportedCredentialTypes types) =>
					{
						return new UsernamePasswordCredentials() { Username = "admin", Password = "admin" };
					},
					OnPushTransferProgress = (int current, int total, long bytes) =>
					{
						onProgress(string.Format("Push{0}/{1},{2}byte", current, total, bytes));
						return true;
					},
					OnNegotiationCompletedBeforePush = (updates) =>
					{
						return true;
					},
					OnPackBuilderProgress = (stage, current, total) =>
					{
						onProgress(string.Format("{0} {1}/{2}", stage, current, total));
						return true;
					},
					OnPushStatusError = (err) =>
					{
						throw new Exception(err.Message);
					}
				});
			}
		}
	}
}
