using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;

namespace ConsoleApplication1
{

	public class Options
	{
		[Option("input", HelpText="Input codesync xml file for reading. Supports .gz internally.")]
		public string InputFile { get;set;}

		[Option("outputdir", HelpText="Directory to output.  Default is same directory as input file (.\\)")]
		public string OutputDir { get;set; }

		[Option("createdir", HelpText="Create directory if neccesary")]
		public bool? CreateDir { get;set; }

		[Option('v', "verbose")]
		public bool Verbose { get;set; }

		[ValueOption(0)]
		public string[] Values { get; set; }
	}
}
