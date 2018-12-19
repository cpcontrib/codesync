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

		static void Main(string[] args)
		{
			string invokedVerb = null;
			object invokedVerbInstance = null;

			CommonOptions Options = null;

			ParserVerbs verbs = new ParserVerbs();
			bool optionsParsed = CommandLine.Parser.Default.ParseArguments(args, verbs,
			  (verb, subOptions) =>
			  {
				  // if parsing succeeds the verb name and correct instance
				  // will be passed to onVerbCommand delegate (string,object)
				  invokedVerb = verb;
				  invokedVerbInstance = subOptions;
				  Options = invokedVerbInstance as CommonOptions;
			  });
			
			if(optionsParsed == false || (Options.Quiet == false && Options.Porcelain == false))
			{
				Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				Console.WriteLine("cpcodesync v{0} (C)Copyright Lightmaker Inc.", v);

				if(optionsParsed==false)
				{
					Console.WriteLine("\tUsage: cpcodesync [verb] [options...]");
					Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
				}
			}


			ICommand cmd = null;
			switch(invokedVerb) 
			{
				case "extract":
					cmd = new ExtractCommand();
					cmd.SetOptions(invokedVerbInstance);
					break;
			}



			//ScanElementsPath(filename, Options);

			//start the command
			TimeSpan writefilesTimeSpan = Timing.ExecuteTimed( ()=>{ cmd.Execute(); } );

			if(Options.Quiet == false && Options.Porcelain == false)
			{
				Console.WriteLine("Completed in {0:0.00} secs", ((float)writefilesTimeSpan.TotalMilliseconds / (float)1000));
			}

			if(Debugger.IsAttached) {
				System.Threading.Tasks.Task.Factory.StartNew(() => { Console.WriteLine("Debugger detected: Press any key to end."); Console.ReadKey(); }).Wait(TimeSpan.FromSeconds(30));
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
