using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CPCodeSyncronize
{
	public class CodeSyncPackageReader : IDisposable
	{

		public CodeSyncPackageReader(string packagePath)
		{
			if(packagePath == null) throw new ArgumentNullException("packagePath");
			this.filename = packagePath;
		}
		public CodeSyncPackageReader(Stream inputStream)
		{
			if(inputStream == null) throw new ArgumentNullException("inputStream");
			this.inputStream = inputStream;
		}

		string filename;
		Stream inputStream;
		bool ownsStream = false;

		public IEnumerable<XElement> GetNodes()
		{
			Stream s = null;
			try
			{
				s = File.OpenRead(filename);

				if(filename.EndsWith(".gz"))
				{
					s = new System.IO.Compression.GZipStream(s, System.IO.Compression.CompressionMode.Decompress);
				}

				return StreamUtils.StreamElements(s, "CodeFile");
				//XDocument xDoc = XDocument.Load(s); s.Close();
				//return xDoc.Element("codeLibrary").Elements("codeFile");
			}
			finally
			{

			}
		}

		public void WriteNodeValueToStream(XElement node, Stream outputStream)
		{
			var nodeEncoding = node.GetAttributeValue("Encoding");
			if(nodeEncoding == null || nodeEncoding == "base64")
			{
				byte[] base64contentgzip = Convert.FromBase64String(node.Value.Trim());

				using(Stream inputStream = GetReadableStream(base64contentgzip))
				{
					int bytesRead = -1;
					do
					{
						bytesRead = inputStream.Read(base64contentgzip, 0, base64contentgzip.Length);
						outputStream.Write(base64contentgzip, 0, bytesRead);
					} while(bytesRead > 0);
				}
			}
			else
			{
				StreamWriter sw = new StreamWriter(outputStream);
				sw.Write(node.Value.Trim());
				sw.Flush();
			}
		}

		public Stream GetReadableStream(byte[] contentBytes)
		{
			Stream inputStream = new MemoryStream(contentBytes);

			if(contentBytes[0] == 0x1f && contentBytes[1] == 0x8b)
			{
				var gzip = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);

				return gzip;
			}

			return inputStream;
		}

		public IEnumerable<string> ScanElementsPath()
		{
			IEnumerable<XElement> codeFileElements = GetNodes();

			List<string> relPaths = new List<string>(100);

			foreach(var filenode in codeFileElements)
			{
				yield return filenode.Attribute("Name").Value;
			}

		}


		public void Dispose()
		{
			
		}
	}

}
