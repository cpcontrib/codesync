using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CPCodeSyncronize.CLI
{
	public class ListCommand : ICommand
	{
		private ListOptions ListOptions;

		public void SetOptions(object value)
		{
			ListOptions = value as ListOptions;
		}

		public void PreExecute()
		{

		}

		public int Execute()
		{
			CodeSyncPackageReader packageReader = new CodeSyncPackageReader(ListOptions.InputFile);

			var relpathList = packageReader.ScanElements((e) => 
				e.GetAttributeValue("Name")
			);

			int count = 0;
			foreach(var relpath in relpathList)
			{
				count++;
				Console.WriteLine("{0}: {1}", count, relpath);
			}

			return 0;
		}
	}
}
