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
		/// <param name="versionBuild">版本定制号. >=0 时,更新版本号; 否则仅获取版本号</param>
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
						if(IsModified && versionBuild >= 0)
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

		/// <summary>
		/// 遍历仓库目录,检索程序集信息
		/// </summary>
		/// <returns>当前仓库程序集列表</returns>
		public static IList<AssemblyInfo> GetInfos(string repoPath)
		{
			var list = new List<AssemblyInfo>();

			//检索C#工程版本配置
			foreach(var item in Directory.GetDirectories(repoPath, "Properties", SearchOption.AllDirectories))
			{
				var file = Path.Combine(item, "AssemblyInfo.cs");
				if(!File.Exists(file))
					continue;

				list.Add(new AssemblyInfo()
				{
					FilePath = file,
					IsModified = false,
					Type = ProjectType.CSharp,
					ProjectPath = Path.GetDirectoryName(item).Substring(repoPath.Length).Replace('\\', '/')
				});
			}

			//检索C工程版本
			foreach(var item in Directory.GetDirectories(repoPath, "Inc", SearchOption.AllDirectories))
			{
				var file = Path.Combine(item, "Version.h");
				if(!File.Exists(file))
					continue;

				list.Add(new AssemblyInfo()
				{
					FilePath = file,
					IsModified = false,
					Type = ProjectType.C,
					ProjectPath = Path.GetDirectoryName(item).Substring(repoPath.Length).Replace('\\', '/')
				});
			}

			return list;
		}

		/// <summary>
		/// 当前工程开发环境
		/// </summary>
		public enum ProjectType { C, CSharp }
	}
}

