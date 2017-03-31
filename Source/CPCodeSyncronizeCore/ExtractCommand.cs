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

			IDictionary<string,bool> existingFiles;
			if(true)
			{
				existingFiles = ReadExistingFiles(state.FullOutputPath);
			}

			WriteFiles(state.InputFile, state.FullOutputPath, ref existingFiles);

			if(true)
			{
				DeleteUnused(state.FullOutputPath, existingFiles);
			}
		}

		private void DeleteUnused(string basepath, IDictionary<string, bool> existingFiles)
		{
			//throw new NotImplementedException();
			var filesToDelete = existingFiles.AsEnumerable().Where(_ => _.Value == false);
			
			if(Options.Quiet==false)
			{
				if(Options.Verbose==false)
				{
					Console.WriteLine("Deleting {0} files.", filesToDelete.Count());
				}
			}

			foreach(var f in filesToDelete)
			{
				if(Options.Verbose)
				{
					Console.WriteLine("Deleting file {0}.", f.Key);
				}
				if(Options.DryRun == false)
				{
					string filepath = Path.Combine(basepath, f.Key);
					try { /*File.Delete(filepath);*/ } catch { }
				}

			}
		}

		private IDictionary<string, bool> ReadExistingFiles(string fullPath, string relPath = null)
		{
			var dirInfo = new DirectoryInfo(fullPath);

			IDictionary<string, bool> retVal = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);


			ReadExistingFiles(dirInfo, null, ref retVal);

			return retVal;
		}

		void ReadExistingFiles(DirectoryInfo dir, string relPath, ref IDictionary<string, bool> retVal)
		{
			if(dir.Exists == true)
			{
				var files = dir.GetFiles();
				if(relPath != null)
				{
					retVal[relPath] = true;
				}

				foreach(var f in files)
				{
					string keyPath = relPath == null ? "" : Path.Combine(relPath, f.Name);
					retVal[keyPath] = false;
				}

				var directories = dir.GetDirectories();
				foreach(var subdir in directories)
				{
					if(subdir.Attributes == FileAttributes.Hidden) continue;
					relPath = relPath == null ? subdir.Name : Path.Combine(relPath, subdir.Name);

					ReadExistingFiles(subdir, relPath, ref retVal);
				}
			}
		}


		private void ListFiles()
		{
			var codeFileNodes = LoadFromFile(state.InputFile);

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


		void WriteFiles(string filename, string outputDir, ref IDictionary<string,bool> existingFiles)
		{
			IEnumerable<XElement> codeFileElements = LoadFromFile(filename);

			if(Options.Verbose)
			{
				Console.WriteLine("Writing files to path '{0}'.", state.FullOutputPath);
			}
			if(Options.Scratch)
			{
				Console.WriteLine("Writing files to scratch path '{0}'.", state.FullOutputPath);
			}

			bool writeTally = (Options.Verbose == false && Options.Quiet == false);
			
			int count=0;
			foreach (var filenode in codeFileElements)
			{
				WriteFile(filenode, outputDir, ref existingFiles);
				count++;

				if(writeTally) Console.Write("Wrote {0} files\r", count);
			}

			if(writeTally) Console.WriteLine();
		}


		void WriteFile(XElement node, string basepath, ref IDictionary<string,bool> existingFiles)
		{
			string name = node.GetAttributeValue("Name");
			string filepath;

			if (name.StartsWith("/"))
				filepath = name.Substring(1).Replace("/", "\\");
			else
				filepath = name.Replace("/", "\\");

			if(Options.DryRun == false)
			{
				DirectoryUtil.EnsureDirectories(filepath, basepath);
			}

			string fullpath = Path.Combine(basepath, filepath);

			WriteFileContent(node, basepath, fullpath);
			if(existingFiles.ContainsKey(filepath) == false)
			{
				existingFiles[filepath] = true;
			}
			else
			{
				existingFiles[filepath] = true;
			}

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

						var nodeEncoding = node.GetAttributeValue("Encoding");
						if (nodeEncoding == null || nodeEncoding == "base64")
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

		
	}

}
