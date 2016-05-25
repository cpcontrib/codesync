using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CPCodeSyncronize
{
	public static class DirectoryUtil
	{

		public static void EnsureDirectories(string filepath, string basepath)
		{
			if(string.IsNullOrEmpty(basepath)) throw new ArgumentException("basePath is empty.");
			if(Directory.Exists(basepath) == false) throw new InvalidOperationException("basepath doesn't exist");

			string[] segments = filepath.Split('\\');

			int index = 0;
			int segmentsLengthMinusOne = segments.Length - 1;

			while(index < segmentsLengthMinusOne)
			{
				string directorypath = Path.Combine(basepath, String.Join("\\", segments.Take(index + 1)));
				index++;

				try
				{
					if(Directory.Exists(directorypath) == false)
					{
						Directory.CreateDirectory(directorypath);
					}
				}
				catch { throw; }
			}
		}


	}
}
