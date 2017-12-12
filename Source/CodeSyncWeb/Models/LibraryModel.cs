using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Serialization;

namespace CodeSyncWeb.Models
{
	
	[DataContract]
	public class CodeLibrary
	{
		[DataMember]
		public ICollection<CodeFile> codeFile { get; set; }
	}

	[DataContract]
	public class CodeFile
	{
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string Encoding { get; set; }

		public string Value { get; set; }
	}
}