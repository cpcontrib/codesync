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

			IDictionary<string,bool> existingFiles;
			if(true)
			{
				existingFiles = ReadExistingFiles(state.FullOutputPath);
			}

			CodeSyncPackageReader packageReader = new CodeSyncPackageReader(state.InputFile);
			WriteFiles(packageReader, state.FullOutputPath, ref existingFiles);

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
			IDictionary<string, bool> existingFiles = null;

			if(Directory.Exists(fullPath))
			{
				var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
				existingFiles = new Dictionary<string, bool>(files.Length, StringComparer.OrdinalIgnoreCase);

				int fullpathSubstringIndex = fullPath.Length + 1;

				foreach(var file in files)
				{
					string shortened = file.Substring(fullpathSubstringIndex);
					existingFiles.Add(shortened, false);
				}
			}

			if(existingFiles == null) existingFiles = new Dictionary<string, bool>();

			return existingFiles;
		}


		void WriteFiles(CodeSyncPackageReader packageReader, string outputDir, ref IDictionary<string,bool> existingFiles)
		{
			IEnumerable<XElement> codeFileElements = packageReader.GetNodes();

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
				WriteFile(packageReader, filenode, outputDir, ref existingFiles);
				count++;

				if(writeTally) Console.Write("Wrote {0} files\r", count);
			}

			if(writeTally) Console.WriteLine();
		}


		void WriteFile(CodeSyncPackageReader packageReader, XElement node, string basepath, ref IDictionary<string,bool> existingFiles)
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

			WriteFileContent(packageReader, node, basepath, fullpath);

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

		void WriteFileContent(CodeSyncPackageReader packageReader, XElement node, string basepath, string fullpath)
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

						packageReader.WriteNodeValueToStream(node, outputStream);

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
