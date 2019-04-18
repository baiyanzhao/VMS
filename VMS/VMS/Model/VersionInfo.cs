using System;
using System.Collections.Generic;

namespace VMS.Model
{
	/// <summary>
	/// 工程版本信息
	/// </summary>
	public class VersionInfo
	{
		public Version VersionNow { get; set; }
		public Version VersionBase { get; set; }
		public string Customer { get; set; }
		public string OrderNumber { get; set; }
		public List<string> KeyWords { get; set; }
		public List<string> VersionList { get; set; }
	}
}
