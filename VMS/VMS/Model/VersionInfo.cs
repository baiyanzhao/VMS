using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VMS.Model
{
	/// <summary>
	/// 工程版本信息
	/// </summary>
	public class VersionInfo
	{
		public class StringProperty
		{
			public string Value { get; set; }
		}

		public class VersionProperty
		{
			public string Label { get; set; }
			public string Title { get; set; }
			public string Value { get; set; }
		}

		public Version VersionNow { get; set; }
		public Version VersionBase { get; set; }
		public string Customer { get; set; }
		public string OrderNumber { get; set; }
		public List<VersionProperty> VersionList { get; set; }
		public ObservableCollection<StringProperty> KeyWords { get; set; }
		[System.Runtime.Serialization.IgnoreDataMember]
		public string CommitMessage { get; set; }
	}
}
