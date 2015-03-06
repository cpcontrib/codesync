﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
	internal static class Timing
	{
		public static TimeSpan ExecuteTimed(Action action)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			action();

			sw.Stop();

			return sw.Elapsed;
		}
	}
}
