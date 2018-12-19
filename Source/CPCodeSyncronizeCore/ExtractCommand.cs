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

		public int Execute()
		{
			if(executestate < 2) PreExecute();

			if(String.IsNullOrEmpty(state.InputFile))
			{
				Console.WriteLine("fail InputFile not specified or --Instance option unable to find a file to use.");
				return 1;
			}
			if(File.Exists(state.InputFile) == false)
			{
				Console.WriteLine("fail InputFile '{0}' doesnt exist.", state.InputFile);
				return 1;
			}

			IDictionary<string,bool> existingFiles = ReadExistingFiles(state.FullOutputPath);

			using(CodeSyncPackageReader packageReader = new CodeSyncPackageReader(state.InputFile))
			{
				WriteFiles(packageReader, state.FullOutputPath, ref existingFiles);

				if(Options.Sync)
				{
					DeleteUnused(state.FullOutputPath, existingFiles);
				}
			}

			return 0;
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
					try { File.Delete(filepath); } catch { }
				}

			}
		}

		private IDictionary<string, bool> ReadExistingFiles(string fullPath, string relPath = null)
		{
			IDictionary<string, bool> existingFiles = null;

			if(Options.Sync && Directory.Exists(fullPath))
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

			//check to see if file in filesystem needs updating (do simple size/datetime check)
			if(ShouldWriteFileContent(packageReader, node, basepath, fullpath))
			{
				WriteFileContent(packageReader, node, basepath, fullpath);
				DateTime lastMod;
				if(DateTime.TryParse(node.GetAttributeValue("LastMod"), out lastMod) == true)
				{
					//LastMod is in UTC from the CMS.
					lastMod = lastMod.ToLocalTime();
					File.SetLastWriteTime(fullpath, lastMod);
				}

				if(Options.Verbose)
					Console.WriteLine(" ok  {0}", fullpath);
			}
			else
			{
				if(Options.Verbose)
					Console.WriteLine("skip {0}", fullpath);

			}

			if(existingFiles.ContainsKey(filepath) == false)
			{
				existingFiles[filepath] = true;
			}
			else
			{
				existingFiles[filepath] = true;
			}


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

		/// <summary>
		/// amount of jitter allowed for lastmod comparisons
		/// </summary>
		TimeSpan lastModJitter = new TimeSpan(0, 0, 2);

		bool ShouldWriteFileContent(CodeSyncPackageReader packageReader, XElement node, string basepath, string fullpath)
		{
			try
			{
				bool shouldWrite = true;

				DateTime cmsLastMod;
				DateTime fsLastMod;
				if(DateTime.TryParse(node.Attribute("LastMod")?.Value, out cmsLastMod))
				{
					fsLastMod = File.GetLastWriteTimeUtc(fullpath);

					if((cmsLastMod - fsLastMod) < lastModJitter)
					{
						shouldWrite = false;
					}
				}

				return shouldWrite;
			}
			catch(Exception ex)
			{
				if(Options.Verbose == false) Console.WriteLine();
				string nameAttrValue = ""; try { nameAttrValue = node.Attribute("Name").Value; }
				catch { }
				Console.Error.WriteLine("Failed on node for file '{0}'.\n{1}", nameAttrValue, ex.ToString());
				Console.Error.WriteLine(node.ToString());
				return true;
			}

		}


	}

}
