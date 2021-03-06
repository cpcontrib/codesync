﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Net.Http;

namespace CPCodeSyncronize.CLI
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
				ExtractCommandBuilder builder = new ExtractCommandBuilder();
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
			if (executestate < 2) PreExecute();

			if (Options.InputFromWeb == true && string.IsNullOrEmpty(Options.Instance)==false)
			{
				//attempt to download from https://codesync.cp-contrib.com/api/v1/library/{InputFile}
				string tempPathFile;
				if (TryDownload(Options.Instance, out tempPathFile) == true)
				{
					state.InputFile = tempPathFile;
				}
			}

			if (String.IsNullOrEmpty(state.InputFile))
			{
				Console.WriteLine("fail InputFile not specified or --Instance option unable to find a file to use.");
				return 1;
			}

			if (File.Exists(state.InputFile) == false)
			{
				Console.WriteLine("fail InputFile '{0}' doesnt exist.", state.InputFile);
				return 1;
			}

			IDictionary<string, bool> existingFiles = ReadExistingFiles(state.FullOutputPath);

			using (CodeSyncPackageReader packageReader = new CodeSyncPackageReader(state.InputFile))
			{
				WriteFiles(packageReader, state.FullOutputPath, ref existingFiles);

				if (Options.Sync)
				{
					DeleteUnused(state.FullOutputPath, existingFiles);
				}
			}

			return 0;
		}

		private static HttpClient S_HttpClient = new HttpClient();

		private bool TryDownload(string instance, out string tempPathFile)
		{
			string url = $"https://codesync.cp-contrib.com/api/v1/library/{instance}";

			if(Options.RefreshLibrary == true)
			{
				url += "?refresh=true";
			}

			if(Options.Quiet==false)
			{
				Console.WriteLine("Download of {0} from codesync.cp-contrib.com...", instance);
				if (Options.RefreshLibrary == true) Console.WriteLine("Refreshing from source...");
			}

			var response = S_HttpClient.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();

			if (response.IsSuccessStatusCode == false) { tempPathFile = null; return false; }

			string savePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), instance);

			using (var fs = File.Create(savePath))
			{
				System.Threading.Tasks.Task.Run(async () =>
				{
					using (var downloadStream = await response.Content.ReadAsStreamAsync())
					{
						byte[] buffer = new byte[64 * 1024]; //64k
						int bytesRead = 0;
						int totalBytes = 0;

						do
						{
							bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length);
							totalBytes += bytesRead;

							if (Options.Quiet == false)
							{
								//keep overwriting bytes read info message
								Console.Write("Read {0} bytes\r", totalBytes);
							}

							await fs.WriteAsync(buffer, 0, bytesRead);


						} while (bytesRead > 0);
						
						//skip a line
						if (Options.Quiet == false) Console.WriteLine();

					}
				}).Wait();
			}

			tempPathFile = savePath;
			return true;
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
			IEnumerable<CodeFileNode> codeFileElements = packageReader.GetCodeFileNodes();

			if(Options.Verbose)
			{
				Console.WriteLine("Writing files to path '{0}'.", state.FullOutputPath);
			}
			if(Options.Scratch)
			{
				Console.WriteLine("Writing files to scratch path '{0}'.", state.FullOutputPath);
			}

			bool writeTally = (Options.Verbose == false && Options.Quiet == false);

			int writeCount = 0;
			int skipCount = 0;

			foreach (var filenode in codeFileElements)
			{
				bool wroteFile = WriteFile(filenode, outputDir, ref existingFiles);

				if(wroteFile)
					writeCount++;
				else
					skipCount++;

				if(writeTally) Console.Write("{0} files\r", writeCount + skipCount);
			}

			if(writeTally) Console.WriteLine("Wrote {0} files, Skipped {1} files", writeCount, skipCount);
		}


		bool WriteFile(CodeFileNode node, string basepath, ref IDictionary<string,bool> existingFiles)
		{
			string name = node.Name;
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

			bool wroteFile;

			//check to see if file in filesystem needs updating (do simple size/datetime check)
			if(ShouldWriteFileContent(node, basepath, fullpath))
			{
				WriteFileContent(node, basepath, fullpath);
				if(node.LastMod != null)
				{
					//LastMod is in UTC from the CMS.
					DateTime lastMod = node.LastMod.Value.ToLocalTime();
					File.SetLastWriteTime(fullpath, lastMod);
				}

				wroteFile = true;

				if(Options.Quiet==false)
					Console.WriteLine("save {0}", node.Name);
			}
			else
			{
				if(Options.Verbose)
					Console.WriteLine("skip {0}", node.Name);

				wroteFile = false;
			}

			if(existingFiles.ContainsKey(filepath) == false)
			{
				existingFiles[filepath] = true;
			}
			else
			{
				existingFiles[filepath] = true;
			}

			return wroteFile;
		}

		private static byte[] S_EmptyByteArray=new byte[0];

		void WriteFileContent(CodeFileNode node, string basepath, string fullpath)
		{
			try
			{
				if (node.HasContent())
				{
					Stream outputStream = null;
					try
					{
						if (Options.DryRun == false)
							outputStream = new FileStream(fullpath, FileMode.Create, FileAccess.Write);
						else
							outputStream = new MemoryStream();

						node.WriteContent(outputStream);

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
				string nameAttrValue = ""; try { nameAttrValue = node.Name; }
				catch { }
				Console.Error.WriteLine("Failed on node for file '{0}'.\n{1}", nameAttrValue, ex.ToString());
				Console.Error.WriteLine(node.ToString());
			}

		}

		/// <summary>
		/// amount of jitter allowed for lastmod comparisons
		/// </summary>
		TimeSpan lastModJitter = new TimeSpan(0, 0, 2);

		bool ShouldWriteFileContent(CodeFileNode node, string basepath, string fullpath)
		{
			try
			{
				bool shouldWrite = true;

				if(node.LastMod.HasValue)
				{
					DateTime cmsLastMod = node.LastMod.GetValueOrDefault();
					DateTime fsLastMod = File.GetLastWriteTimeUtc(fullpath);

					if((cmsLastMod - fsLastMod) < lastModJitter)
					{
						shouldWrite = false;
					}
				}

				return shouldWrite;
			}
			catch 
			{
				//Status.Warn(ex);
				return true;
			}

		}


	}

}
