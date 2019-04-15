using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMS.Model
{
	/// <summary>
	/// 工程版本信息
	/// </summary>
    class VersionInfo
    {
		public string VersionNow { get; set; }
		public string VersionBase { get; set; }
		public string Customer { get; set; }
		public string OrderNumber { get; set; }
		public List<string> KeyWords { get; set; }
		public List<string> VersionList { get; set; }
	}
}
