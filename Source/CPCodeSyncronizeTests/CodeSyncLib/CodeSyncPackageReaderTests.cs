using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CPCodeSyncronizeTests.CodeSyncLib
{
	[TestFixture]
	public class CodeSyncPackageReaderTests
	{

		const string XML1 = @"<CodeLibrary><CodeFile Name=""/System"">{CodeFileContent}</CodeFile></CodeLibrary>";
		
		const string STRING1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string STRING1_BASE64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVo=";
		const string STRING1_GZIP_BASE64 = "H4sIAIe14lgAA3N0cnZxdXP38PTy9vH18w8IDAoOCQ0Lj4iMAgAiePerGgAAAA==";

		[Test]
		public void GetReadableStream_FromText()
		{
			byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(STRING1);

			var packageReader = new CPCodeSyncronize.CodeSyncPackageReader("");

			var readstream = packageReader.GetReadableStream(contentBytes);

			var buffer = new byte[1000];
			var readbytes = readstream.Read(buffer, 0, 1000);

			var actual = System.Text.Encoding.UTF8.GetString(buffer,0,readbytes);
			Assert.That(actual, Is.EqualTo(STRING1));
		}

		//[Test]
		//public void GetReadableStream_FromBase64Text()
		//{
		//	byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(STRING1_BASE64);

		//	var packageReader = new CPCodeSyncronize.CodeSyncPackageReader("");

		//	var readstream = packageReader.GetReadableStream(contentBytes);

		//	var buffer = new byte[1000];
		//	var readbytes = readstream.Read(buffer, 0, 1000);

		//	var actual = System.Text.Encoding.UTF8.GetString(buffer, 0, readbytes);
		//	Assert.That(actual, Is.EqualTo(STRING1));
		//}

		[Test]
		public void GetReadableStream_FromGZippedText()
		{
			byte[] contentBytes = Convert.FromBase64String(STRING1_GZIP_BASE64);

			var packageReader = new CPCodeSyncronize.CodeSyncPackageReader("");

			var readstream = packageReader.GetReadableStream(contentBytes);

			var buffer = new byte[1000];
			var readbytes = readstream.Read(buffer, 0, 1000);

			var actual = System.Text.Encoding.UTF8.GetString(buffer, 0, readbytes);
			Assert.That(actual, Is.EqualTo(STRING1));
		}

		private MemoryStream CreateFromText(string encodingtype, string text)
		{
			var str = XML1
					.Replace(">{CodeFileContent}", string.Format(" Encoding=\"{0}\">", encodingtype)+"{CodeFileContent}")
					.Replace("{CodeFileContent}", text);

			return new MemoryStream(
				System.Text.Encoding.UTF8.GetBytes(
					str
				)
			);
		}

		[Test]
		public void GetNodes_Base64Encoded()
		{
			var ms = CreateFromText("Text",STRING1);

			var packageReader = new CPCodeSyncronize.CodeSyncPackageReader(ms);

			var nodes = packageReader.GetNodes().ToList();

			var actual = nodes[0].Value;
			Assert.That(actual, Is.EqualTo(STRING1));
		}
		[Test]
		public void GetNodes_GZipBase64Encoded()
		{
			var ms = CreateFromText("base64/gzip", STRING1);

			var packageReader = new CPCodeSyncronize.CodeSyncPackageReader(ms);

			var nodes = packageReader.GetNodes().ToList();

			var actual = nodes[0].Value;
			Assert.That(actual, Is.EqualTo(STRING1));
		}

	}
}
