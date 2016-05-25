using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApplication1
{
	public class ExtractCommand
	{

		public ExtractOptions Options;

		public void PreExecute()
		{

		}

		public void Execute()
		{
			string filename;

			//temp hack for listing
			if(Options.List==true)
			{
				ListFiles();
				return;
			}

			string fullUri = Options.InputFile;// "http://dev-retailnationalgrid.nationalgridaccess.com/codelibrary.xml";
			if (Options.OutputDir != null && Directory.Exists(Options.OutputDir) == false)
			{
				if (Options.CreateDir != false)
				{
					if (Options.CreateDir == null)
					{
						Console.Write("basepath '{0}' doesn't exist, would you like to create it? (y/n)", Options.OutputDir);
						var key = Console.ReadKey();
						Console.WriteLine();
						if (key.Key == ConsoleKey.Y) Options.CreateDir = true;
					}

					if (Options.CreateDir == true)
					{
						if (Options.Verbose) Console.WriteLine("Creating directory '{0}'.", Options.OutputDir);
						if (Options.DryRun == false) Directory.CreateDirectory(Options.OutputDir);
					}
				}
			}

			if (fullUri.StartsWith("http://") || fullUri.StartsWith("https://"))
			{
				filename = fullUri.Substring(fullUri.LastIndexOf("/") + 1);
				if (File.Exists(filename) == false)
				{

					System.Net.WebClient wc = new System.Net.WebClient();
					wc.DownloadFile(fullUri, filename);
				}
			}
			else
			{
				filename = Options.InputFile;
			}

			string outputDir = Path.GetFullPath(Options.OutputDir ?? ".");

			{
				Options.OutputDir = Path.GetDirectoryName(filename);
				if (Options.OutputDir == "")
				{
					Options.OutputDir = ".\\";
				}
			}

			if (Options.Verbose)
			{
				Console.WriteLine("OutputDir: {0}", Options.OutputDir);
			}
			WriteFiles(filename, outputDir);
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

			int count=0;
			foreach (var filenode in codeFileElements)
			{
				WriteFile(filenode, outputDir);
				count++;
				if(Options.Verbose==false)
					Console.Write("{0} files\r", count);
				
			}
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
