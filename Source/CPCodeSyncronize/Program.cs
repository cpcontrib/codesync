using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;

namespace ConsoleApplication1
{
	class Program
	{

		static ExtractOptions Options;

		static void Main(string[] args)
		{
			Options = new ExtractOptions();
			CommandLine.Parser.Default.ParseArguments(args, Options); 
			//, () => { 
			//	CommandLine.Text.HelpText.AutoBuild(Options); 
			//	if(Debugger.IsAttached) Console.ReadKey();
			//	Environment.Exit(1); 
			//});

			string filename;

			string fullUri = args[0];// "http://dev-retailnationalgrid.nationalgridaccess.com/codelibrary.xml";
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
						if(Options.Verbose) Console.WriteLine("Creating directory '{0}'.", Options.OutputDir);
						if(Options.DryRun==false) Directory.CreateDirectory(Options.OutputDir);
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
				filename = args[0];
			}

			if(Options.OutputDir == null)
			{
				Options.OutputDir = Path.GetDirectoryName(filename);
				if(Options.OutputDir == "")
				{
					Options.OutputDir = ".\\";
				}
			}

			if(Options.Verbose)
			{
				Console.WriteLine("OutputDir: {0}", Options.OutputDir);
			}

			//ScanElementsPath(filename, Options);

			TimeSpan writefilesTimeSpan = ExecuteTimed( ()=>{ WriteFiles(filename); } );

			if(Options.Verbose==false) Console.WriteLine();
			Console.WriteLine("Completed in {0:0.00} secs", ((float)writefilesTimeSpan.TotalMilliseconds / (float)1000));

			if(Debugger.IsAttached) {
				System.Threading.Tasks.Task.Factory.StartNew(() => Console.ReadKey()).Wait(TimeSpan.FromSeconds(10));
			}
		}

		static void ScanElementsPath(string filename, ExtractOptions options)
		{
			IEnumerable<XElement> codeFileElements = LoadFromFile(filename);

			List<string> relPaths = new List<string>(100);

			foreach (var filenode in codeFileElements)
			{
				relPaths.Add(filenode.Attribute("name").Value);
			}

		}

		static TimeSpan ExecuteTimed(Action action)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			action();

			sw.Stop();

			return sw.Elapsed;
		}

		static void WriteFiles(string filename)
		{
			IEnumerable<XElement> codeFileElements = LoadFromFile(filename);

			foreach (var filenode in codeFileElements)
			{
				WriteFile(filenode, Options.OutputDir);
			}
		}

		static IEnumerable<XElement> LoadFromFile(string filename)
		{
			Stream s = null;
			try
			{
				s = File.OpenRead(filename);

				if(filename.EndsWith(".gz"))
				{
					s = new System.IO.Compression.GZipStream(s, System.IO.Compression.CompressionMode.Decompress);
				}

				return StreamCodeFileElements(s, "codeFile");
			}
			finally
			{
				
			}
		}

		static IEnumerable<XElement> StreamCodeFileElements(Stream source, string elementName)
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
					if (reader.NodeType == XmlNodeType.Element
						&& reader.Name == elementName)
					{
						item = XElement.ReadFrom(reader) as XElement;

						if (item != null) yield return item;
					}
				}
			}
		}

		static void WriteFile(XElement node, string basepath)
		{
			try
			{
				string name = node.Attribute("name").Value;

				string filepath;

				if (name.StartsWith("/"))
					filepath = name.Substring(1).Replace("/", "\\");
				else
					filepath = name.Replace("/", "\\");

				EnsureDirectories(filepath, basepath);

				string fullpath = Path.Combine(basepath, filepath);

				if(node.Value != "")
				{ 
					byte[] base64contentgzip = Convert.FromBase64String(node.Value.Trim());

					Stream outputStream=null;
					try
					{
						if(Options.DryRun == false) 
							outputStream = new FileStream(fullpath, FileMode.Create, FileAccess.Write);
						else
							outputStream = new MemoryStream();
							
						using (Stream inputStream = PrepareInputStream(base64contentgzip))
						{
							int bytesRead = -1;
							do
							{
								bytesRead = inputStream.Read(base64contentgzip, 0, base64contentgzip.Length);
								if(Options.DryRun==false) outputStream.Write(base64contentgzip, 0, bytesRead);
							} while (bytesRead > 0);
						}
							
						outputStream.Flush();
					}
					finally
					{
						if(outputStream!=null) outputStream.Dispose();
					}
				}
				else
				{
					//Log.Warn("file node '{0}' contained no content.", node.Attribute("name").Value);
				}

				if(Options.Verbose)
					Console.WriteLine("Wrote file '{0}'.", fullpath);
				else
					Console.Write(".");
			}
			catch (Exception ex)
			{
				if(Options.Verbose==false) Console.WriteLine();
				string nameAttrValue=""; try { nameAttrValue = node.Attribute("name").Value; } catch {}
				Console.Error.WriteLine("Failed on node for file '{0}'.\n{1}", nameAttrValue, ex.ToString());
				Console.Error.WriteLine(node.ToString());
			}

		}

		static void EnsureDirectories(string filepath, string basepath)
		{
			if(Options.DryRun) return;
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

		static Stream PrepareInputStream(byte[] contentBytes)
		{
			Stream inputStream = new MemoryStream(contentBytes);

			if(contentBytes[0] == 0x1f && contentBytes[1] == 0x8b)
			{
				var gzip = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);

				return gzip;
			}

			return inputStream;
		}

















		static void Main2(string[] args)
		{

			var files = System
				.IO
				.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory);

			System.Text.StringBuilder sb = new StringBuilder();

			foreach (string file in files)
			{
				WriteFileNode(file, sb);
			}

			using (StreamWriter sw = new StreamWriter(".\\out.xml"))
			{
				sw.Write(sb.ToString());
			}

		}

		static void WriteFileNode(string path, System.Text.StringBuilder sb)
		{
			sb.AppendFormat("<codeFile name=\"{0}\">", path);

			var fs = System
			.IO
			.File.OpenRead(path);
			byte[] bytes = new byte[fs.Length];
			int bytesRead = fs.Read(bytes, 0, (int)fs.Length);
			sb.Append(Convert.ToBase64String(bytes, 0, bytes.Length));

			sb.AppendLine("\n\t</codeFile>\n");
		}

	}


}
