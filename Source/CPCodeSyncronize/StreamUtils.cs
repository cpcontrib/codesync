using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApplication1
{
	internal class StreamUtils
	{

		public static IEnumerable<XElement> StreamElements(Stream source, string elementName)
		{
			using (XmlReader reader = XmlReader.Create(source))
			{
				XElement item = null;

				reader.MoveToContent();

				// Parse the file, save header information when encountered, and yield the
				// Item XElement objects as they are created.

				// loop through codeFile elements
				while (reader.Read())
				{
					readnext:
					if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
					{

						item = XElement.ReadFrom(reader) as XElement;

						if (item != null) 
						{
							yield return item;
							goto readnext;
						}
					}
				}
			}
		}

	}

}
