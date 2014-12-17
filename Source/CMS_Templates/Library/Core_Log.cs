using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrownPeak.CMSAPI;
using CrownPeak.CMSAPI.Services;
/* Some Namespaces are not allowed. */
namespace CrownPeak.CMSAPI.CustomLibrary
{
	public class Log
	{
		internal static bool LevelDebug = false;
		internal static bool LevelTrace = false;

		#region Logging
		public static void Trace(string message)
		{
			if (LevelTrace) Out.DebugWriteLine(message);
		}
		public static void Trace(string format, params object[] args)
		{
			if (LevelTrace) Out.DebugWriteLine(format, args);
		}
		public static void Trace(Func<string> func)
		{
			if (LevelTrace) Out.DebugWriteLine(func());
		}
		public static void Debug(string message)
		{
			if (LevelDebug) Out.DebugWriteLine(message);
		}
		public static void Debug(string format, params object[] args)
		{
			if (LevelDebug) Out.DebugWriteLine(format, args);
		}
		public static void Debug(Func<string> func)
		{
			if (LevelDebug) Out.DebugWriteLine(func());
		}
		public static void Warn(string format, params object[] args)
		{
			/*if(LevelWarn)*/
			Out.DebugWriteLine(format, args);
		}
		public static void Warn(string message)
		{
			/*if(LevelWarn)*/
			Out.DebugWriteLine(message);
		}
		#endregion

		public static void Error(string message, Exception ex)
		{
			Out.DebugWriteLine("[error] {0}: {1}", message, ex.StackTrace);
		}
		public static void Error(Exception ex)
		{
			Out.DebugWriteLine(ex.ToString());
		}

		public static void Info(string message)
		{
			Out.DebugWriteLine("[info] {0}", message);
		}
		public static void Info(string format, params object[] args)
		{
			Out.DebugWriteLine("[info] " + format, args);
		}
	}
}
