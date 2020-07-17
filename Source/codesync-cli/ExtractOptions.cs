
using CommandLine;

namespace CPCodeSyncronize.CLI
{

	[Verb("extract")]
	public class ExtractOptions : CommonOptions
	{
		[Option("instance", HelpText = "Instance name to pull file from. Uses configuration and current directory to read from and write to.")]
		public string Instance { get; set; }

		[Option("input", HelpText="Input codesync xml file for reading. Detects gzip encoding automatically.")]
		public string InputFile { get;set;}

		[Option('o', "outputdir", HelpText="Directory to output.  Default is same directory as input file (.\\)")]
		public string OutputDir { get;set; }

		[Option("createdir", HelpText="Create directory if neccesary")]
		public bool? CreateDir { get;set; }

		[Option('s', "scratch")]
		public bool Scratch { get; set; }

		[Option("input-from-web")]
		public bool InputFromWeb { get; set; }

		[Option("refresh-library")]
		public bool RefreshLibrary { get; set; }

		//[ValueList(typeof(string))]
		//public IList<string> Values { get; set; }

		//[ParserState]
		//public IParserState LastParserState { get;set; }

		[Option("sync", HelpText="Syncronize (mirror) the package", Default=true)]
		public bool Sync { get; set; }
	}
}
