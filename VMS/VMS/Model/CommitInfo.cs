using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMS.Data
{
	abstract class CommitInfo
	{
		public string Sha { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public string Message { get; set; }
		public Version Version { get; set; }
		public DateTimeOffset When { get; set; }
	}
}
