using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;

using CommandLine;

namespace ConsoleApplication1
{
	using CPCodeSyncronize.CLI;

	class Program
	{

		static int Main(string[] args)
		{
			ICommand cmd = null;
			CommonOptions Options = null;

			int exitcode = CommandLine.Parser.Default.ParseArguments<ExtractOptions, ListOptions>(args)
			  .MapResult(
				(ExtractOptions o) => {
					cmd = new ExtractCommand();
					cmd.SetOptions(o);
					Options = o;
					return 0; },
				(ListOptions o) => {
					cmd = new ListCommand();
					cmd.SetOptions(o);
					Options = o;
					return 0; },
				(errs) => 1
			  );
			if(exitcode > 0) return exitcode;

			/*
			if(optionsParsed == false || (Options.Quiet == false && Options.Porcelain == false))
			{
				Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				Console.Error.WriteLine("cpcodesync v{0} (C)2018", v);

				
			}
			*/
			
			//start the command
			TimeSpan writefilesTimeSpan = Timing.ExecuteTimed(()=> { exitcode = cmd.Execute(); });

			if(Options.Quiet == false && Options.Porcelain == false)
			{
				Console.WriteLine("Completed in {0:0.00} secs", ((float)writefilesTimeSpan.TotalMilliseconds / (float)1000));
			}

			if(Debugger.IsAttached) {
				System.Threading.Tasks.Task.Factory.StartNew(() => { Console.WriteLine("Debugger detected: Press any key to end."); Console.ReadKey(); }).Wait(TimeSpan.FromSeconds(30));
			}

			return exitcode;
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
