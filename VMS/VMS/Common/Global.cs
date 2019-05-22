using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;
using VMS.Model;

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

	static class Global
	{
		const string FILE_VERSION_INFO = "Version.json";		//定制信息
		//const string FILE_PRESET = ".\\Config\\Preset.json";   //预置
		const string FILE_SETTING_LOCAL = "Config\\Setting.json";  //设置

		//static Preset _preset;
		public static Setting Setting;
		public static readonly string FILE_SETTING = ApplicationDeployment.IsNetworkDeployed ? Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, FILE_SETTING_LOCAL) : FILE_SETTING_LOCAL;

		static Global()
		{
			//配置文件
			try
			{
				//_preset = new JavaScriptSerializer().Deserialize<Preset>(File.ReadAllText(FILE_PRESET));
				Setting = new JavaScriptSerializer().Deserialize<Setting>(File.ReadAllText(FILE_SETTING));
			}
			catch(Exception)
			{ }

			//配置默认值
			//_preset = _preset ?? new Preset();
			//_preset.Users = _preset.Users ?? new List<Preset.User> { new Preset.User { Name = "Root" }, new Preset.User { Name = "User" } };
			//File.WriteAllText(FILE_PRESET, new JavaScriptSerializer().Serialize(_preset));

			Setting = Setting ?? new Setting();
			Setting.PackageFolder = Setting.PackageFolder ?? @"D:\Package\";
			Setting.RepoUrl = Setting.RepoUrl ?? @"http://admin:admin@192.168.1.49:2507/r/Straw.git";
			Setting.CompareToolPath = Setting.CompareToolPath ?? @"D:\Program Files\Beyond Compare 4\BCompare.exe";
			Setting.LoaclRepoPath = Setting.LoaclRepoPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VMS\";
		}

		public static string MakeText<T, TProperty>(this T p, Expression<Func<T, TProperty>> e)
		{
			return (e.Body as MemberExpression)?.Member.Name;
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

				var lines = File.ReadAllLines(FilePath, Encoding.UTF8);
				for(int i = 0; i < lines.Length; i++)
				{
					if(lines[i].IndexOf(verKey) == 0)
					{
						var strVersion = lines[i].Substring(verKey.Length).Split(new char[] { '\"' })[0];
						if(System.Version.TryParse(strVersion, out System.Version version))
						{
							if(IsModified)
							{
								int revision = version.Build == versionBuild ? version.Revision + 1 : 0;
								Version = (new System.Version(version.Major, version.Minor, versionBuild, revision));
								lines[i] = lines[i].Replace(strVersion, Version.ToString());
								File.WriteAllLines(FilePath, lines, Encoding.UTF8);
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
		static bool WriteObject<T>(string path, T val) where T : class
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
		static T ReadObject<T>(string path) where T : class
		{
			T val = default(T);
			if(!File.Exists(path))
				return val;

			try
			{
				using(Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					val = new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
				}
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
		public static VersionInfo ReadVersionInfo() => ReadObject<VersionInfo>(Path.Combine(Setting.LoaclRepoPath, FILE_VERSION_INFO));

		/// <summary>
		/// 工程版本信息
		/// </summary>
		public static VersionInfo ReadVersionInfo(string sha)
		{
			try
			{
				using(var repo = new Repository(Setting.LoaclRepoPath))
				{
					return ReadVersionInfo(repo.Lookup<Commit>(sha));
				}
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
			try
			{
				var obj = commit.Tree["Version.json"]?.Target as Blob;
				return obj == null ? null : new DataContractJsonSerializer(typeof(VersionInfo)).ReadObject(obj.GetContentStream()) as VersionInfo;
			}
			catch
			{ }

			return null;
		}

		/// <summary>
		/// 更新版本,并写入文件
		/// </summary>
		/// <param name="info"></param>
		public static void WriteVersionInfo(VersionInfo info) => WriteObject(Path.Combine(Setting.LoaclRepoPath, FILE_VERSION_INFO), info);

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
					Repository.Clone(Setting.RepoUrl, Setting.LoaclRepoPath);
				}

				//同步仓库,并推送当前分支
				using(var repo = new Repository(Setting.LoaclRepoPath))
				{
					Commands.Fetch(repo, "origin", new string[0], null, null);
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
			public static void Commit(Repository repo, string message)
			{
				Commands.Stage(repo, "*");
				var sign = new Signature(Setting.User, Environment.MachineName, DateTime.Now);
				repo.Commit(message, sign, sign);
				repo.Network.Push(repo.Head, new PushOptions()
				{
					CredentialsProvider = (string url, string usernameFromUrl, SupportedCredentialTypes types) =>
					{
						return new UsernamePasswordCredentials() { Username = "admin", Password = "admin" };
					},
					CertificateCheck = (certificate, valid, host) =>
					{
						return true;
					},
					OnPushTransferProgress = (int current, int total, long bytes) =>
					{
						return true;
					},
					OnNegotiationCompletedBeforePush = (updates) =>
					{
						return true;
					},
					OnPackBuilderProgress = (stage, current, total) =>
					{
						return true;
					},
					OnPushStatusError = (updates) =>
					{
						return;
					}
				});
			}
		}
	}
}
