
using CommandLine;

namespace CPCodeSyncronize.CLI
{
	public class CommonOptions
	{

		[Option('v', "verbose")]
		public bool Verbose { get; set; }

		[Option('q', "quiet", Default = false)]
		public bool Quiet { get; set; }

		[Option('d', "dryrun")]
		public bool DryRun { get; set; }

		[Option('p', "porcelain")]
		public bool Porcelain { get; set; }


	}

}
