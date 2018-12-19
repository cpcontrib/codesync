using System;
using System.IO;
using System.Linq;
using System.Text;

using CPCodeSyncronize;
using NUnit.Framework;

namespace CPCodeSyncronizeTests
{
	[TestFixture]
	public class CodeSyncPackageReader_Tests
	{
		const string XML1 = @"<CodeLibrary><CodeFile Name=""/System"">{CodeFileContent}</CodeFile></CodeLibrary>";
		
		private CodeSyncPackageReader CreateReader(string xml)
		{
			MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
			return new CodeSyncPackageReader(ms);
		}
		private MemoryStream CreateFromText(string encodingtype, string text)
		{
			var str = XML1
					.Replace(">{CodeFileContent}", string.Format(" Encoding=\"{0}\">", encodingtype) + "{CodeFileContent}")
					.Replace("{CodeFileContent}", text);

			return new MemoryStream(
				System.Text.Encoding.UTF8.GetBytes(
					str
				)
			);
		}

		[Test]
		public void ctor_InputStream_Shouldnt_Get_Closed()
		{
			byte[] bytes = Encoding.UTF8.GetBytes(XML1);
			MemoryStream ms = new MemoryStream();
			ms.Write(bytes, 0, bytes.Length);

			var reader = new CodeSyncPackageReader(ms);
			var nodes = reader.GetNodes()
				.ToList(); //force complete enumeration

			Assert.DoesNotThrow(() => {
				//check all operations: read/seek/write
				ms.Seek(0, SeekOrigin.Begin);
				ms.Read(bytes, 0, 2);
				ms.Seek(0, SeekOrigin.End);
				ms.WriteByte(0x42);
			});
		}
	}
}
