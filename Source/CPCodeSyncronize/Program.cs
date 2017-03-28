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
	using CPCodeSyncronize;

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
			Options.InputFile = args[0];

			if(Options.Quiet == false && Options.Porcelain == false)
			{
				Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				Console.WriteLine("cpcodesync v{0} (C)Copyright Lightmaker Inc.", v);
				
				Console.WriteLine("\tUsage: cpcodesync [verb] [options...]");
			}

			//ScanElementsPath(filename, Options);

			ICommand cmd = new ExtractCommand();
			cmd.SetOptions(Options);

			//start the command
			TimeSpan writefilesTimeSpan = Timing.ExecuteTimed( ()=>{ cmd.Execute(); } );

			if(Options.Quiet == false && Options.Porcelain == false)
			{
				Console.WriteLine("Completed in {0:0.00} secs", ((float)writefilesTimeSpan.TotalMilliseconds / (float)1000));
			}

			if(Debugger.IsAttached) {
				System.Threading.Tasks.Task.Factory.StartNew(() => { Console.WriteLine("Debugger detected: Press any key to end."); Console.ReadKey(); }).Wait(TimeSpan.FromSeconds(10));
			}
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
