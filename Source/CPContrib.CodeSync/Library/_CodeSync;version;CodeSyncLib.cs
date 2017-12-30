using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CrownPeak.CMSAPI;

namespace CMS_Templates.Library
{
	public class CmsDirectoryTraverser
	{

		public CmsDirectoryTraverser(Asset startingPath, IEnumerable<string> includePathsSpec = null, IEnumerable<string> ignorePathsSpec = null)
		{
			this._startingPath = startingPath;

			this._includePathsSpec = includePathsSpec;
			if(_includePathsSpec == null) _includePathsSpec = new string[0];

			this._ignorePathsSpec = ignorePathsSpec;
			if(_ignorePathsSpec == null) _ignorePathsSpec = new string[0];
		}

		private Asset _startingPath;

		private IEnumerable<string> _includePathsSpec;
		private IEnumerable<string> _ignorePathsSpec;

		public IEnumerable<Asset> Traverse()
		{
			List<Asset> totalFiles = new List<Asset>(100);
			
			foreach(var IncludePathSpec in _includePathsSpec)
			{
				Asset folder = Asset.Load(IncludePathSpec);
				if(folder.IsLoaded)
					_Traverse(folder, ref totalFiles);
			}
			
			return totalFiles;
		}

		private void _Traverse(Asset folder, ref List<Asset> totalFiles)
		{
			AssetParams ap = new AssetParams() { ExcludeProjectTypes = false };

			List<Asset> files = folder.GetFileList(ap);
			foreach(var file in files)
			{
				string assetPathStr = file.AssetPath.ToString();
				if(CheckExcludedPath(assetPathStr) == false)
					totalFiles.Add(file);
			}
		}

		public bool CheckIncludedPath(string assetPathStr)
		{
			foreach(var includePathSpec in _includePathsSpec)
			{
				if(assetPathStr.StartsWith(includePathSpec) == true) return true;
			}

			return false;
		}

		public bool CheckExcludedPath(string assetPathStr)
		{
			foreach(var ignorePathSpec in _ignorePathsSpec)
			{
				if(assetPathStr.StartsWith(ignorePathSpec) == true) return true;
			}

			return false;
		}

		private class Traverser : IEnumerator<Asset>
		{
			public Traverser(Asset folder)
			{
				this._folder = folder;
			}
			private Asset _Current;
			public Asset Current
			{
				get { return _filesEnumerator.Current; }
			}

			public void Dispose()
			{
				if(_filesEnumerator!=null) _filesEnumerator.Dispose();
			}

			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}

			Asset _folder;
			List<Asset> _files;
			IEnumerator<Asset> _filesEnumerator;

			public bool MoveNext()
			{
				if(_filesEnumerator == null)
				{
					AssetParams ap = new AssetParams() { ExcludeProjectTypes = false };
					_files = _folder.GetFileList(ap);
					_filesEnumerator = _files.GetEnumerator();
				}

				return _filesEnumerator.MoveNext();
			}

			public void Reset()
			{
				_filesEnumerator.Reset();
			}
		}

	}

}