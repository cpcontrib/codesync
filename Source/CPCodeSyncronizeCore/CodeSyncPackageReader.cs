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
				if(this.inputStream != null)
				{
					s = inputStream; 
				}
				else if(this.filename != null)
				{
					s = File.OpenRead(filename);
					ownsStream = true;
				}

				if(s.CanSeek == false)
				{
					s = new BufferedStream(s);
				}

				byte[] markers = new byte[2];
				s.Read(markers, 0, 2);
				s.Seek(0, SeekOrigin.Begin);

				if(markers[0] == 0x1f && markers[1] == 0x8b)
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
					inputStream.CopyTo(outputStream);
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
