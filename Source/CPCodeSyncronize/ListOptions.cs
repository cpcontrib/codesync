
using CommandLine;

namespace CPCodeSyncronize.CLI
{

	[Verb("list")]
	public class ListOptions : CommonOptions
	{

		[Value(0, Required=true)]
		public string InputFile { get; set; }

	}
}
