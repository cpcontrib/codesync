using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CPCodeSyncronize
{
	public interface ICommand
	{
		void SetOptions(object value);

		void PreExecute();

		int Execute();

	}
}
