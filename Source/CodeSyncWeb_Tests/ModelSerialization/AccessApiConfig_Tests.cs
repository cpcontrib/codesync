using CodeSyncWeb.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSyncWeb_Tests
{
	[TestFixture]
	public class AccessApiConfig_Tests : ModelSerialization_Tests<AccessApiConfig>
	{



		[Test]
		public void Can_Deserialize()
		{
			var instances = DeserializeJson<IList<Instance>>("~/App_Data/accessapi-config.json");

			Assert.Pass();
		}


	}

	public abstract class ModelSerialization_Tests<T>
	{
		public ModelSerialization_Tests()
		{
			_TestAssembly = System.Reflection.Assembly.GetExecutingAssembly();
		}
		private System.Reflection.Assembly _TestAssembly;

		protected T DeserializeJson(string projectFileName)
		{
			using(StreamReader sr = new StreamReader(FileHelper.GetStream(_TestAssembly, projectFileName)))
			{
				using(var reader = new JsonTextReader(sr))
				{
					object val = JsonSerializer.Create().Deserialize<T>(reader);
					return (T)val;
				}
			}
		}
		protected Talt DeserializeJson<Talt>(string projectFileName)
		{
			using(StreamReader sr = new StreamReader(FileHelper.GetStream(_TestAssembly, projectFileName)))
			{
				using(var reader = new JsonTextReader(sr))
				{
					object val = JsonSerializer.Create().Deserialize<Talt>(reader);
					return (Talt)val;
				}
			}
		}

		protected T DeserializeXml(string projectFileName)
		{
			throw new NotImplementedException();
		}

		

	}

	public static class ContractX
	{
		public static T ArgNotNull<T>(T value, string paramName)
		{
			if(value == null) throw new ArgumentNullException(paramName);
			return value;
		}

	}

	public static class FileHelper
	{
		public static Stream GetStream(System.Reflection.Assembly testAssembly, string projectFileName)
		{
			ContractX.ArgNotNull(testAssembly, nameof(testAssembly));
			ContractX.ArgNotNull(projectFileName, nameof(projectFileName));

			string fname, path;
			


			//look for resource first
			fname = projectFileName;
			if(fname.StartsWith("~/")) fname = fname.Substring(2);
			if(fname.IndexOf("/") > -1) fname = fname.Replace("/", ".");
			//check for assembly manifest resource names


			//setup basePath for rest of checks
			string basePath = Path.GetDirectoryName(new Uri(testAssembly.CodeBase).LocalPath);
			
			//look for dll local file next (has Copy To Output Directory set)
			fname = projectFileName;
			if(fname.StartsWith("~/")) fname = fname.Substring(2);
			if(fname.IndexOf("/") > -1) fname = fname.Replace("/", "\\");

			path = Path.Combine(basePath, fname);
			if(File.Exists(path))
			{
				return File.OpenRead(path);
			}

			//look for file higher up in project folder, if we running in bin/Debug or bin/Release
			path = Path.GetFullPath(Path.Combine(basePath, "..", "..", fname));
			if(File.Exists(path))
			{
				return File.OpenRead(path);
			}

			throw new ApplicationException($"Failed to find the file '${projectFileName}'.");
		}
	}
}
