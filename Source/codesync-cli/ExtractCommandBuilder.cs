using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CPCodeSyncronize.CLI
{
	public class ExtractCommandBuilder
	{

		public ExtractCommand.ExtractState ReadOptions(ExtractOptions Options)
		{
			var state = new ExtractCommand.ExtractState();

			if(Options.Porcelain)
			{
				Options.Quiet = true;
				Options.Verbose = false;
			}

			ReadInputFile(Options, ref state);
			ReadOutputDir(Options, ref state);

			ReadInstance(Options, ref state);

			if(string.IsNullOrEmpty(state.FullOutputPath))
			{
				state.FullOutputPath = Path.GetFullPath(state.OutputDir);
			}

			return state;
		}

		private void ReadInstance(ExtractOptions Options, ref ExtractCommand.ExtractState state)
		{
			if(string.IsNullOrEmpty(Options.Instance)==false)
			{
				string[] inputFileFormats = new string[] { "{0}.xml.gz", "{0}.xml" };

				foreach(var format in inputFileFormats.AsEnumerable())
				{
					//set input file to configuration location default.
					string inputFile = string.Format(format, Options.Instance);

					if(File.Exists(inputFile))
					{
						state.InputFile = inputFile;
					}

					//set output directory to current directory + instance name
					if(String.IsNullOrEmpty(state.OutputDir))
						state.OutputDir = string.Format(@".\{0}", Options.Instance);
				}
			}

		}

		private void ReadInputFile(ExtractOptions Options, ref ExtractCommand.ExtractState state)
		{
			if(string.IsNullOrEmpty(Options.InputFile) == false)
			{
				string fullUri = Options.InputFile;

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
		}

		private void ReadOutputDir(ExtractOptions Options, ref ExtractCommand.ExtractState state)
		{
			if(Options.Scratch == true)
			{
				state.FullOutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("d"));
				state.OutputDir = state.FullOutputPath;

				if(Directory.Exists(state.FullOutputPath)==false)
				{
					if(Options.DryRun == false) Directory.CreateDirectory(state.FullOutputPath);
				}

				if(Options.Porcelain)
				{
					Console.WriteLine("CS0005: Writing to scratch directory=\"{0}\"", state.FullOutputPath);
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
							Console.Write("Directory '{0}' doesn't exist, would you like to create it? (y/n)", Path.GetFullPath(Options.OutputDir));
							var key = Console.ReadKey();
							Console.WriteLine();
							if(key.Key == ConsoleKey.Y) Options.CreateDir = true;
						}

						if(Options.CreateDir == true)
						{
							if(Options.Verbose && Options.Quiet == false) Console.WriteLine("Creating directory '{0}'.", Path.GetFullPath(Options.OutputDir));
							if(Options.DryRun == false) Directory.CreateDirectory(Options.OutputDir);
						}
					}

				}

				state.OutputDir = Options.OutputDir;
			}

		}
	}
}
