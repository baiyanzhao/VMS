using System;
using System.Collections.Generic;

namespace VMS.Model
{
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
		public string MSBuildPath { get; set; }
	}
}
