using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CPCodeSyncronize;
using NUnit.Framework;

namespace CPCodeSyncronizeTests.CodeSyncLib
{
	[TestFixture]
	public class CodeFileNode_Tests
	{

		
		const string STRING1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string STRING1_BASE64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVo=";
		const string STRING1_GZIP_BASE64 = "H4sIAIe14lgAA3N0cnZxdXP38PTy9vH18w8IDAoOCQ0Lj4iMAgAiePerGgAAAA==";

		private CodeFileNode CreateNodeValue(string value, string encoding=null)
		{
			XElement node = new XElement("CodeFile", 
				new XAttribute("LastMod", DateTime.UtcNow.ToString("O")),
				value);

			if(encoding != null)
				node.Add(new XAttribute("Encoding", encoding));

			return new CodeFileNode(node);
		}

		[Test]
		[TestCase(STRING1)]
		[TestCase(STRING1_GZIP_BASE64)]
		public void WriteContent_Text_NullEncoding(string encodedstring)
		{
			//arrange
			var codefilenode = CreateNodeValue(encodedstring);
			var expected = encodedstring;

			//act
			MemoryStream ms = new MemoryStream();
			codefilenode.WriteContent(ms);

			ms.Seek(0, SeekOrigin.Begin);
			var actual = System.Text.Encoding.UTF8.GetString(ms.ToArray());

			//assert
			Assert.That(actual, Is.EqualTo(expected));
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
		public void WriteContent_GZippedText()
		{
			//arrange
			var codefilenode = CreateNodeValue(STRING1_GZIP_BASE64, "base64");

			//act
			MemoryStream ms = new MemoryStream();
			codefilenode.WriteContent(ms);

			ms.Seek(0, SeekOrigin.Begin);
			var actual = System.Text.Encoding.UTF8.GetString(ms.ToArray());

			//assert
			Assert.That(actual, Is.EqualTo(STRING1));
		}



	}
}
