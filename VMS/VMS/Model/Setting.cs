using System;
using System.Collections.Generic;

namespace VMS.Model
{
	/// <summary>
	/// 预置
	/// </summary>
	public class Preset
	{
		public List<User> Users { get; set; }
		public List<string> Lables { get; set; }

		public class User
		{
			public string Name { get; set; }
			public string Password { get; set; }
		}
	}

	/// <summary>
	/// 配置
	/// </summary>
	public class Setting
	{
		public string User { get; set; }
		public string RepoUrl { get; set; }
		public string LoaclRepoPath { get; set; }
		public string PackageFolder { get; set; }
		public string CompareToolPath { get; set; }
	}

	/// <summary>
	/// 分支定制信息
	/// </summary>
	public class SpecialInfo
	{
		public Version Version { get; set; }
		public DateTime CreateTime { get; set; }
		public string OrderNumber { get; set; }
		public List<string> Lable { get; set; }
	}
}
