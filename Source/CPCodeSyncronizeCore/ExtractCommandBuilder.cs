using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CPCodeSyncronize.Core
{
	public class ExtractCommandBuilder
	{

		public CPCodeSyncronize.ExtractCommand.ExtractState ReadOptions(ExtractOptions Options)
		{
			var state = new CPCodeSyncronize.ExtractCommand.ExtractState();

			ReadInputFile(Options, ref state);
			ReadOutputDir(Options, ref state);

			return state;
		}

		private void ReadInputFile(ExtractOptions Options, ref CPCodeSyncronize.ExtractCommand.ExtractState state)
		{
			string fullUri = Options.InputFile;// "http://dev-retailnationalgrid.nationalgridaccess.com/codelibrary.xml";

			string filename;
			if(fullUri.StartsWith("http://") || fullUri.StartsWith("https://"))
			{
				state.InputFromWeb = true;

				filename = fullUri.Substring(fullUri.LastIndexOf("/") + 1);
				if(File.Exists(filename) == false)
				{

					System.Net.WebClient wc = new System.Net.WebClient();
					wc.DownloadFile(fullUri, filename);
				}
			}
			else
			{
				filename = Options.InputFile;
			}

			state.InputFile = filename;
		}

		private void ReadOutputDir(ExtractOptions Options, ref CPCodeSyncronize.ExtractCommand.ExtractState state)
		{
			if(Options.Scratch == true)
			{
				state.FullOutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("d"));
				state.OutputDir = state.FullOutputPath;

				if(Directory.Exists(state.FullOutputPath)==false)
				{
					if(Options.DryRun == false) Directory.CreateDirectory(state.FullOutputPath);
				}
			}
			else
			{
				if(Options.OutputDir != null && Directory.Exists(Options.OutputDir) == false)
				{
					if(Options.CreateDir != false)
					{
						if(Options.CreateDir == null)
						{
							Console.Write("basepath '{0}' doesn't exist, would you like to create it? (y/n)", Options.OutputDir);
							var key = Console.ReadKey();
							Console.WriteLine();
							if(key.Key == ConsoleKey.Y) Options.CreateDir = true;
						}

						if(Options.CreateDir == true)
						{
							if(Options.Verbose && Options.Quiet == false) Console.WriteLine("Creating directory '{0}'.", Options.OutputDir);
							if(Options.DryRun == false) Directory.CreateDirectory(Options.OutputDir);
						}
					}

				}

				state.OutputDir = Options.OutputDir ?? ".";

				state.FullOutputPath = Path.GetFullPath(state.OutputDir);
			}
		}
	}
}
