using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CPCodeSyncronize
{
	public static class XLinqExtensions
	{

		public static string GetAttributeValue(this XElement node, string key)
		{
			XAttribute attr = node.Attribute(key);
			if(attr != null) return attr.Value;
			else return null;
		}

	}
}
