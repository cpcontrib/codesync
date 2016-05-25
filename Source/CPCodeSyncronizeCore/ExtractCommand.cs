using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CPCodeSyncronize
{
	public class ExtractCommand : ICommand
	{

		private ExtractOptions Options;
		void ICommand.SetOptions(object value)
		{
			this.Options = (ExtractOptions)value;
		}

		private int executestate = 0;
		public void PreExecute()
		{
			executestate = 1;
			try
			{
				CPCodeSyncronize.Core.ExtractCommandBuilder builder = new Core.ExtractCommandBuilder();
				this.state = builder.ReadOptions(this.Options);
				executestate = 2;
			}
			catch
			{
				executestate = 3; throw;
			}
		}

		public class ExtractState
		{
			public string OutputDir;
			public string FullOutputPath;
			public string InputFile;
			public bool InputFromWeb;
		}

		private ExtractState state;

		public void Execute()
		{
			if(executestate < 2) PreExecute();

			//temp hack for listing
			if(Options.List==true)
			{
				ListFiles();
				return;
			}

			WriteFiles(state.InputFile, state.FullOutputPath);
		}


		private void ListFiles()
		{
			var codeFileNodes = LoadFromFile(Options.InputFile);

			int count = 0;
			foreach(var node in codeFileNodes)
			{
				count++;
				Console.WriteLine("{0}: {1}", count, node.Attribute("name").Value);
			}
		}

		static IEnumerable<XElement> LoadFromFile(string filename)
		{
			Stream s = null;
			try
			{
				s = File.OpenRead(filename);

				if (filename.EndsWith(".gz"))
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

		static Stream PrepareInputStream(byte[] contentBytes)
		{
			Stream inputStream = new MemoryStream(contentBytes);

			if (contentBytes[0] == 0x1f && contentBytes[1] == 0x8b)
			{
				var gzip = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);

				return gzip;
			}

			return inputStream;
		}

		static void ScanElementsPath(string filename, ExtractOptions options)
		{
			IEnumerable<XElement> codeFileElements = LoadFromFile(filename);

			List<string> relPaths = new List<string>(100);

			foreach (var filenode in codeFileElements)
			{
				relPaths.Add(filenode.Attribute("Name").Value);
			}

		}


		void WriteFiles(string filename, string outputDir)
		{
			IEnumerable<XElement> codeFileElements = LoadFromFile(filename);

			if(Options.Verbose)
			{
				Console.WriteLine("Writing files to path '{0}'.", state.FullOutputPath);
			}

			int count=0;
			foreach (var filenode in codeFileElements)
			{
				WriteFile(filenode, outputDir);
				count++;
				
				if(Options.Verbose==false) Console.Write("Wrote {0} files\r", count);
			}

			if(Options.Verbose == false && Options.Quiet == false) Console.WriteLine();
		}


		void WriteFile(XElement node, string basepath)
		{
			string name = node.GetAttributeValue("Name");

			string filepath;

			if (name.StartsWith("/"))
				filepath = name.Substring(1).Replace("/", "\\");
			else
				filepath = name.Replace("/", "\\");

			EnsureDirectories(filepath, basepath);

			string fullpath = Path.Combine(basepath, filepath);

			WriteFileContent(node, basepath, fullpath);

			DateTime lastMod;
			if(DateTime.TryParse(node.GetAttributeValue("lastMod"), out lastMod)==true)
			{
				File.SetLastWriteTime(fullpath, lastMod);
			}

			if (Options.Verbose)
				Console.WriteLine("Wrote file '{0}'.", fullpath);
		}

		private static byte[] S_EmptyByteArray=new byte[0];

		void WriteFileContent(XElement node, string basepath, string fullpath)
		{
			try
			{
				if (node.Value != "")
				{
					Stream outputStream = null;
					try
					{
						if (Options.DryRun == false)
							outputStream = new FileStream(fullpath, FileMode.Create, FileAccess.Write);
						else
							outputStream = new MemoryStream();

						if (node.GetAttributeValue("Encoding") == "base64")
						{
							byte[] base64contentgzip = Convert.FromBase64String(node.Value.Trim());

							using (Stream inputStream = PrepareInputStream(base64contentgzip))
							{
								int bytesRead = -1;
								do
								{
									bytesRead = inputStream.Read(base64contentgzip, 0, base64contentgzip.Length);
									if (Options.DryRun == false) outputStream.Write(base64contentgzip, 0, bytesRead);
								} while (bytesRead > 0);
							}
						}
						else
						{ 
							StreamWriter  sw=new StreamWriter(outputStream);
							sw.Write(node.Value.Trim());
							sw.Flush();
						}

						outputStream.Flush();
					}
					finally
					{
						if (outputStream != null) outputStream.Dispose();
					}
				}
				else
				{
					File.WriteAllBytes(fullpath, S_EmptyByteArray); //Log.Warn("file node '{0}' contained no content.", node.Attribute("name").Value);
				}
			}
			catch (Exception ex)
			{
				if (Options.Verbose == false) Console.WriteLine();
				string nameAttrValue = ""; try { nameAttrValue = node.Attribute("Name").Value; }
				catch { }
				Console.Error.WriteLine("Failed on node for file '{0}'.\n{1}", nameAttrValue, ex.ToString());
				Console.Error.WriteLine(node.ToString());
			}

		}

		void EnsureDirectories(string filepath, string basepath)
		{
			if (Options.DryRun) return;
			if (string.IsNullOrEmpty(basepath)) basepath = ".\\";
			if (Directory.Exists(basepath) == false) throw new InvalidOperationException("basepath doesn't exist");

			string[] segments = filepath.Split('\\');

			int index = 0;
			int segmentsLengthMinusOne = segments.Length - 1;

			while (index < segmentsLengthMinusOne)
			{
				string directorypath = Path.Combine(basepath, String.Join("\\", segments.Take(index + 1)));
				index++;

				try
				{
					if (Directory.Exists(directorypath) == false)
					{
						Directory.CreateDirectory(directorypath);
					}
				}
				catch { throw; }
			}
		}

	}

}
