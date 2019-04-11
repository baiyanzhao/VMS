using System;
using System.Windows.Input;

namespace VMS.Data
{
	/// <summary>
	/// Git Tag信息
	/// </summary>
	class BranchInfo
	{
		public string Sha { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public string Message { get; set; }
		public Version Version { get; set; }
		public DateTimeOffset When { get; set; }
	}
}
