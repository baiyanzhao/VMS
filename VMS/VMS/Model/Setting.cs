using System.Collections.Generic;

namespace VMS.Model
{
	/// <summary>
	/// 配置
	/// </summary>
	public class Setting
	{
		public string User { get; set; }
		public List<string> RepoPathList { get; set; }
		public string PackageFolder { get; set; }
		public string CompareToolPath { get; set; }
		public List<string> LatestMessage { get; set; }
		public Dictionary<(string Url, string UsernameFromUrl), (string User, string Password)> CredentialPairs { get; set; }
		public bool IsAutoCommit { get; set; }
		public bool IsTipsCommit { get; set; }
		public bool IsDirectExit { get; set; }
	}
}
