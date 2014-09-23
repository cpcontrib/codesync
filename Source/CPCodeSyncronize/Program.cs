﻿using System;
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
		static string SafeArgs(string[] args, int index, string defaultValue=null)
		{
			if (index > args.Length-1)
				return defaultValue;
			else
				return args[index];
		}
		static void Main(string[] args)
		{
			

			string filename;
			string basepath = SafeArgs(args,1);
			if(string.IsNullOrEmpty(basepath)) basepath = ".\\";

			string fullUri = args[0];// "http://dev-retailnationalgrid.nationalgridaccess.com/codelibrary.xml";
			if(Directory.Exists(basepath)==false)
			{
				Console.Write("basepath '{0}' doesn't exist, would you like to create it? (y/n)", basepath);
				var key = Console.ReadKey();
				Console.WriteLine();
				if (key.Key == ConsoleKey.Y)
					Directory.CreateDirectory(basepath);
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

			IEnumerable<XElement> codeFileElements = LoadFromFile(filename);

			foreach (var filenode in codeFileElements)
			{
				WriteFile(filenode, basepath);
			}

			if(Debugger.IsAttached) Console.ReadKey();
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

				byte[] base64contentgzip = Convert.FromBase64String(node.Value.Trim());

				using (var outputStream = File.OpenWrite(fullpath))
				{

					using (Stream inputStream = PrepareInputStream(base64contentgzip))
					{
						int bytesRead = -1;
						do
						{
							bytesRead = inputStream.Read(base64contentgzip, 0, base64contentgzip.Length);
							outputStream.Write(base64contentgzip, 0, bytesRead);
						} while (bytesRead > 0);
					}

				}

				Console.WriteLine("Wrote file '{0}'.", fullpath);
			}
			catch (Exception ex)
			{
				string nameAttrValue=""; try { nameAttrValue = node.Attribute("name").Value; } catch {}
				Console.Error.WriteLine("Failed on node for file '{0}'.\n{1}", nameAttrValue, ex.ToString());
				Console.Error.WriteLine(node.ToString());
			}

		}

		static void EnsureDirectories(string filepath, string basepath)
		{
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
