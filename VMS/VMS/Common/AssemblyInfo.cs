using System;
using System.Collections.Generic;
using System.IO;

namespace VMS
{
	/// <summary>
	/// 程序集信息
	/// </summary>
	public class AssemblyInfo
	{
		/// <summary>
		/// 工程标识
		/// </summary>
		private TypeMark Mark { get; set; }

		/// <summary>
		/// 版本配置文件的绝对路径
		/// </summary>
		private string FilePath { get; set; }

		/// <summary>
		/// 工程文件夹的相对路径
		/// </summary>
		public string ProjectPath { get; set; }

		/// <summary>
		/// 工程存在修改的文件
		/// </summary>
		public bool IsModified { get; set; }

		/// <summary>
		/// 当前版本
		/// </summary>
		public System.Version Version { get; set; }

		/// <summary>
		/// 工程标题
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// 工程修改时间
		/// </summary>
		public string Time { get; set; }

		/// <summary>
		/// 工程标识列表
		/// </summary>
		private static readonly List<TypeMark> TypeMarkList = new List<TypeMark>();

		static AssemblyInfo()
		{
			/// C工程版本格式为: static const char VERSION[] = "1.0.0.0";
			/// C#工程版本格式为: [assembly: AssemblyFileVersion("1.3.0.0")]
			TypeMarkList.Clear();
			TypeMarkList.Add(new TypeMark { Type = TypeMark.ProjectType.C, Directory = "Inc", File = "Version.h", TitleKey = "static const char TITLE[] = \"", VersionKey = "static const char VERSION[] = \"", AssemblyKey = null, TimeKey="static const char UPDATE_TIME[] = \"" });
			TypeMarkList.Add(new TypeMark { Type = TypeMark.ProjectType.CSharp, Directory = "Properties", File = "AssemblyInfo.cs", TitleKey = "[assembly: AssemblyTitle(\"", VersionKey = "[assembly: AssemblyFileVersion(\"", AssemblyKey = "[assembly: AssemblyVersion(\"", TimeKey= "[assembly: AssemblyTitle(\"" });
		}

		/// <summary>
		/// 更新当前版本,如果工程修改则递增Revision,并修改Build,同时更新相应文件
		/// </summary>
		/// <param name="versionBuild">版本定制号. >=0 时,更新版本号; 否则仅获取版本号</param>
		public void HitVersion(int versionBuild)
		{
			var encoding = FileEncoding.EncodingType.GetType(FilePath);
			var lines = File.ReadAllLines(FilePath, encoding);
			for(var i = 0; i < lines.Length; i++)
			{
				/// 工程标题
				if(Mark.TitleKey != null && lines[i].IndexOf(Mark.TitleKey) == 0)
				{
					Title = lines[i].Substring(Mark.TitleKey.Length).Split(new char[] { '\"' })[0];
				}

				/// 程序集版本
				if(Mark.AssemblyKey != null && lines[i].IndexOf(Mark.AssemblyKey) == 0)
				{
					var strVersion = lines[i].Substring(Mark.AssemblyKey.Length).Split(new char[] { '\"' })[0];
					if(System.Version.TryParse(strVersion, out var version))
					{
						if(IsModified && versionBuild >= 0)
						{
							var revision = version.Build == versionBuild ? version.Revision + 1 : 0;
							version = (new System.Version(version.Major, version.Minor, versionBuild, revision));
							lines[i] = lines[i].Replace(strVersion, version.ToString());
						}
					}
				}

				/// 文件版本
				if(Mark.VersionKey != null && lines[i].IndexOf(Mark.VersionKey) == 0)
				{
					var strVersion = lines[i].Substring(Mark.VersionKey.Length).Split(new char[] { '\"' })[0];
					if(System.Version.TryParse(strVersion, out var version))
					{
						if(IsModified && versionBuild >= 0)
						{
							var revision = version.Build == versionBuild ? version.Revision + 1 : 0;
							Version = (new System.Version(version.Major, version.Minor, versionBuild, revision));
							lines[i] = lines[i].Replace(strVersion, Version.ToString());
						}
						else
						{
							Version = version;
						}
					}
				}

				/// 更新时间
				if(Mark.TimeKey != null && lines[i].IndexOf(Mark.TimeKey) == 0)
				{
					var strTime = lines[i].Substring(Mark.TimeKey.Length).Split(new char[] { '\"' })[0];
					if(IsModified && versionBuild >= 0)
					{
						Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
						lines[i] = lines[i].Replace(strTime, Time);
					}
					else
					{
						Time = strTime;
					}
				}
			}
			File.WriteAllLines(FilePath, lines, encoding);
		}

		/// <summary>
		/// 遍历仓库目录,检索程序集信息
		/// </summary>
		/// <returns>当前仓库程序集列表</returns>
		public static IList<AssemblyInfo> GetInfos(string repoPath)
		{
			var list = new List<AssemblyInfo>();
			if(repoPath == null)
				return list;

			//检索工程版本配置
			foreach(var mark in TypeMarkList)
			{
				foreach(var item in Directory.GetDirectories(repoPath, mark.Directory, SearchOption.AllDirectories))
				{
					var file = Path.Combine(item, mark.File);
					if(!File.Exists(file))
						continue;

					list.Add(new AssemblyInfo()
					{
						Mark = mark,
						FilePath = file,
						IsModified = false,
						ProjectPath = Path.GetDirectoryName(item).Substring(repoPath.Length).Replace('\\', '/')
					});
				}
			}

			return list;
		}

		/// <summary>
		/// 工程类型标识
		/// </summary>
		private class TypeMark
		{
			/// <summary>
			/// 工程类型
			/// </summary>
			public ProjectType Type { get; set; }

			/// <summary>
			/// 标识文件所在文件夹
			/// </summary>
			public string Directory { get; set; }

			/// <summary>
			/// 标识文件名称
			/// </summary>
			public string File { get; set; }

			/// <summary>
			/// 工程标题关键字
			/// </summary>
			public string TitleKey { get; set; }

			/// <summary>
			/// 程序集版本关键字
			/// </summary>
			public string AssemblyKey { get; set; }

			/// <summary>
			/// 文件版本关键字
			/// </summary>
			public string VersionKey { get; set; }

			/// <summary>
			/// 文件版本关键字
			/// </summary>
			public string TimeKey { get; set; }

			/// <summary>
			/// 当前工程开发环境
			/// </summary>
			public enum ProjectType { C, CSharp }
		}
	}
}
