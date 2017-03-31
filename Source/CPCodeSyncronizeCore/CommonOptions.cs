using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;

namespace CPCodeSyncronize
{
	public class CommonOptions
	{

		[Option('v', "verbose")]
		public bool Verbose { get; set; }

		[Option('q', "quiet", DefaultValue = false)]
		public bool Quiet { get; set; }

		[Option('d', "dryrun")]
		public bool DryRun { get; set; }

		[Option('p', "porcelain")]
		public bool Porcelain { get; set; }


	}

	public class ParserVerbs
	{

		[VerbOption("extract", HelpText = "Extract contents of a cpcodesync generated file")]
		public ExtractOptions ExtractVerb { get; set; }

		[VerbOption("list", HelpText = "List contents of a cpcodesync generated file")]
		public ListOptions ListVerb { get; set; }

	}
}
