using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace CPCodeSyncronize
{
	public class CodeFileNode
	{
		public CodeFileNode(XElement node)
		{
			this._node = node;

			DateTime lastMod;
			if(DateTime.TryParse(node.GetAttributeValue("LastMod"), out lastMod) == true)
			{
				_lastMod = lastMod;
			}
		}

		private XElement _node;

		public string Name { get => _node.GetAttributeValue("Name"); }

		DateTime? _lastMod;

		public DateTime? LastMod { get => _lastMod.Value; }

		public bool HasContent()
		{
			return (string.IsNullOrWhiteSpace(_node.Value)==false);
		}

		public void WriteContent(Stream outputStream)
		{
			var nodeEncoding = _node.GetAttributeValue("Encoding");
			if(nodeEncoding != null)
			{
				if(nodeEncoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
				{
					byte[] base64contentgzip = Convert.FromBase64String(_node.Value.Trim());

					using(Stream inputStream = GetReadableStream(base64contentgzip))
					{
						inputStream.CopyTo(outputStream);
					}

					return;
				}

				//if(nodeEncoding=="somethingelse")
				//{
				//	return;
				//}
			}

			//using(StreamWriter sw = new StreamWriter(outputStream, Encoding.UTF8, 2^16, false))
			StreamWriter sw = new StreamWriter(outputStream);
			{
				sw.Write(_node.Value);
				sw.Flush();
			}
		}


		private Stream GetReadableStream(byte[] contentBytes)
		{
			//new CryptoStream()
			Stream inputStream = new MemoryStream(contentBytes);

			//check for GZIP header
			if(contentBytes[0] == 0x1f && contentBytes[1] == 0x8b)
			{
				//wrap inputStream with a gzip decompresser
				inputStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
			}

			return inputStream;
		}

	}
}
