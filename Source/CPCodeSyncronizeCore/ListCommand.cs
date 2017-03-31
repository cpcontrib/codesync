﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CPCodeSyncronize
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

		public void Execute()
		{
			CodeSyncPackageReader packageReader = new CodeSyncPackageReader(ListOptions.InputFile);

			var relpathList = packageReader.ScanElementsPath();

			int count = 0;
			foreach(var relpath in relpathList)
			{
				count++;
				Console.WriteLine("{0}: {1}", count, relpath);
			}

		}
	}
}