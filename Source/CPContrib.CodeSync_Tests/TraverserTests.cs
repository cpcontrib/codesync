using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS_Templates.Library;
using NUnit.Framework;

namespace CPCodeSyncronizeTests.CodeSyncLib
{
	[TestFixture] 
	public class TraverserTests
	{

		[Test]
		[TestCase("/System/Templates/AdventGeneral")]
		public void Traverser_CheckExcludedPaths(string ignorePath1)
		{
			var traverser = new CmsDirectoryTraverser(null, new string[] { "/System/Library", "/System/Templates" }, new string[] { ignorePath1 });

			var filelist = Class1.GetFileList();
			
			bool[] set1 = filelist.Select(_=>_.StartsWith(ignorePath1)).ToArray();
			bool[] set2 = new bool[filelist.Count()];

			for(int i = 0; i < filelist.Length; i++ )
			{
				set2[i] = traverser.CheckExcludedPath(filelist[i]);
			}

			Assert.That(set1, Is.EquivalentTo(set2));
		}

		[Test]
		[TestCase("/System/Templates/AdventGeneral")]
		public void Traverser_CheckIncludePaths(string ignorePath1)
		{
			var traverser = new CmsDirectoryTraverser(null, new string[] { "/System/Library", "/System/Templates" }, new string[] { ignorePath1 });

			var filelist = Class1.GetFileList();

			bool[] set1 = filelist.Select(_ => _.StartsWith(ignorePath1)).ToArray();
			bool[] set2 = new bool[filelist.Count()];

			for(int i = 0; i < filelist.Length; i++)
			{
				set2[i] = traverser.CheckExcludedPath(filelist[i]);
			}

			Assert.That(set1, Is.EquivalentTo(set2));
		}

		[Test]
		[TestCase("/System/Library", "/System/Templates/AdventGeneral")]
		public void Traverser_CheckPaths(string includedPaths, string ignorePaths)
		{
			var traverser = new CmsDirectoryTraverser(null, includedPaths.Split(','), ignorePaths.Split(','));

			var filelist = Class1.GetFileList();

			string[] expected = @"
/System/Library/some_class.cs
/System/Templates/_LmCore/TemplateX/output.aspx
/System/Templates/IncludeThis/input.aspx
/System/Templates/IncludeThis/output.aspx
/System/Templates/IncludeThis/post_save.aspx".Split('\n');

			List<string> set2 = new List<string>();

			for(int i = 0; i < filelist.Length; i++)
			{
				if(traverser.CheckIncludedPath(filelist[i]) && traverser.CheckExcludedPath(filelist[i]) == false)
					set2.Add(filelist[i]);
			}

			var actual = set2;

			Assert.That(expected, Is.EquivalentTo(actual));
		}

	}
}
