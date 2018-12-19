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
			ownsStream = false;
		}

		string filename;
		Stream inputStream;
		bool ownsStream = false;

		/// <summary>
		/// Creates an enumeration of CodeFile elements in the given file.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<XElement> GetNodes()
		{
			Stream s = null;
			try
			{
				s = _OpenStream();
				
				return StreamUtils.StreamElements(s, "CodeFile");
			}
			finally
			{

			}
		}

		private Stream _OpenStream()
		{
			Stream s;

			if(this.filename != null)
			{
				s = File.OpenRead(filename);
				ownsStream = true;
			}
			else
			{
				s = inputStream;
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

			return s;
		}

		/// <summary>
		/// Gets an enumerable list from <see cref="GetNodes"/>
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CodeFileNode> GetCodeFileNodes()
		{
			var nodesEnumerable = GetNodes();
			foreach(XElement node in nodesEnumerable)
			{
				yield return new CodeFileNode(node);
			}
		}




		public IEnumerable<T> ScanElements<T>(Func<XElement, T> item)
		{
			IEnumerable<XElement> codeFileElements = GetNodes();

			foreach(var filenode in codeFileElements)
			{
				T returnval = item(filenode);
				yield return returnval;
			}

		}


		public void Dispose()
		{
			if(inputStream!=null && ownsStream==true)
			{
				IDisposable disposable = inputStream as IDisposable;
				if(disposable != null) disposable.Dispose();
			}
			GC.SuppressFinalize(this);
		}
	}

}
