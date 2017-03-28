using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrownPeak.CMSAPI;
using CrownPeak.CMSAPI.Services;
/* Some Namespaces are not allowed. */

namespace CrownPeak.CMSAPI.CustomLibrary
{

	public static class Util2
	{
		public static Dictionary<string, string> MakeDictionary(params string[] values)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (string value in values) dictionary.Add(value, value);
			return dictionary;
		}

		public static string GetBaseAssetPath(AssetPath path)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("/");
			for (int i = 0; i < path.Count - 1; i++)
			{
				sb.Append(path[i]); sb.Append("/");
			}
			return sb.ToString();
		}

	}

	/// <summary>
	/// Helper class for dealing with InputHelper.ShowLink() content fields
	/// </summary>
	public static class Links
	{

		public static class LinkTypes
		{
			public const string Internal = "internal",
										  External = "external",
										  TitleOnly = "title only",
										  None = "None";
		}

		#region "InputHelper"

		/// <summary>
		/// 
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="fieldName"></param>
		/// <param name="fieldLabel"></param>
		/// <param name="fieldMessage"></param>
		/// <param name="useShortTitle"></param>
		/// <param name="targetBlankExternal"></param>
		/// <param name="targetBlankInternal"></param>
		/// <param name="noTitles"></param>
		/// <param name="headerColor"></param>
		/// <param name="addLinkDescription"></param>
		/// <param name="customLabel"></param>
		/// <param name="defaultFolder"></param>
		public static void ShowLinkInput(Asset asset, String fieldName, String fieldLabel = "", String fieldMessage = "", bool useShortTitle = false, bool targetBlankExternal = false, bool targetBlankInternal = false,
										 bool noTitles = false, String headerColor = "", bool addLinkDescription = false, String customLabel = "Link Type", string defaultFolder = null,
										 string defaultType = ""
										, bool showQueryParameters = false)
		{
			if (!String.IsNullOrWhiteSpace(fieldLabel)) { Input.ShowHeader(fieldLabel, headerColor); }
			if (!String.IsNullOrWhiteSpace(fieldMessage)) { Input.ShowMessage(fieldMessage); }

			Input.StartDropDownContainer(customLabel, fieldName + "_type",
										 new string[] { LinkTypes.Internal, LinkTypes.External, LinkTypes.TitleOnly }.ToDictionary(_ => _),
										 defaultValue: defaultType,
										 firstRowLabel: LinkTypes.None);
			{
				//internal link
				if (useShortTitle)
				{
					Input.ShowMessage("Internal Links get their title from the asset's \"Short Title\"");
				}
				else
				{
					if (!noTitles)
					{
						Input.ShowTextBox("Title", fieldName + "_internal_title", width: 45, helpMessage: "Link Text");
					}
				}
				ShowAcquireParams aParams = new ShowAcquireParams();
				aParams.ShowUpload = false;
				aParams.DefaultFolder = asset.Parent.AssetPath.ToString();

				Input.ShowAcquireDocument("Target", fieldName + "_internal_link", aParams, "<br />Select internal asset.");

				if (showQueryParameters)
				{
					Input.StartExpandPanel("Query Parameters");
					{
						Input.ShowTextBox("Query String", fieldName + "_internal_linkquery", helpMessage: "{field}={value}&...&{fieldn}={valuen}");
						Input.ShowTextBox("Query Fragment", fieldName + "_internal_linkquery_fragment", helpMessage: "{field}={value}&...&{fieldn}={valuen}");
					}
					Input.EndExpandPanel();
				}

				if (targetBlankInternal) { Input.ShowCheckBox("", fieldName + "_internal_tblank", "true", "Open in new window?"); }
			}

			Input.NextDropDownContainer();
			{
				//external link
				if (!noTitles)
				{
					Input.ShowTextBox("Title", fieldName + "_external_title", width: 45, helpMessage: "Link Text");
				}
				Input.ShowTextBox("URL", fieldName + "_external_link", "#", width: 45, helpMessage: "Enter external URL.");

				if (targetBlankExternal) { Input.ShowCheckBox("", fieldName + "_external_tblank", "true", "Open in new window?"); }
			}

			Input.NextDropDownContainer();
			{
				//title only
				Input.ShowTextBox("Title", fieldName + "_titleonly_title", width: 45, helpMessage: "Text");
			}

			Input.EndDropDownContainer();

			if (addLinkDescription)
			{
				Input.ShowTextBox("Description", fieldName + "_link_description", width: 45, height: 5, helpMessage: "<br />Enter Link Description (optional).");
			}
		}

		#endregion

		#region "OutputHelper"

		/// <summary>
		/// Gets the type of link, internal or external.
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public static string GetLinkType(IFieldAccessor asset, string fieldName)
		{
			return asset.Raw(fieldName + "_type");
		}

		/// <summary>
		/// Gets the target href for the link
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public static string GetHref(IFieldAccessor asset, string fieldName, bool addDomain = false, ProtocolType protocolType = ProtocolType.Http)
		{
			string href = null;

			switch (asset[fieldName + "_type"])
			{
				case "internal":
					Asset targetAsset = GetTargetAsset(asset, fieldName);
					if (targetAsset.IsLoaded)
					{
						href = targetAsset.GetLink(addDomain, protocolType);

						//Ability to add query string parameters to the internal asset.
						if (!String.IsNullOrWhiteSpace(href) && !String.IsNullOrWhiteSpace(asset.Raw(fieldName + "_internal_linkquery")))
						{
							href = href + (href.IndexOf("?") > 0 ? "&" : "?") + Uri.EscapeUriString(asset.Raw(fieldName + "_internal_linkquery"));
						}
						if (!String.IsNullOrWhiteSpace(href) && !String.IsNullOrWhiteSpace(asset.Raw(fieldName + "_internal_linkquery_fragment")))
						{
							href = href + (href.IndexOf("#") > 0 ? "&" : "#") + Uri.EscapeUriString(asset.Raw(fieldName + "_internal_linkquery_fragment"));
						}
					}
					break;
				case "external":
					href = asset[fieldName + "_external_link"];
					try
					{
						href = new Uri(href).ToString();
					}
					catch (UriFormatException) { }
					catch (ArgumentNullException) { }
					break;
			}

			Log.Debug("GetHref: returning '{0}'.", href);
			return href;
		}

		/// <summary>
		/// Retrieves a suitable value for placing in a Hyperlink Target attribute.
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="fieldName">The fieldname specified on InputHelper.ShowLink</param>
		/// <returns></returns>
		public static string GetHyperlinkTarget(IFieldAccessor asset, string fieldName)
		{
			string target = "";

			switch (asset[fieldName + "_type"])
			{
				case "internal":
					target = asset[fieldName + "_internal_tblank"];
					if (target == "true") target = "_blank";
					break;
				case "external":
					target = asset[fieldName + "_external_tblank"];
					if (target == "true") target = "_blank";
					break;
			}

			return target;
		}

		/// <summary>
		/// Gets the NavigateUrl for the link, suitable for ASP.Net server controls.
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="fieldName">The fieldname specified on InputHelper.ShowLink</param>
		/// <returns></returns>
		public static string GetNavigateUrl(IFieldAccessor asset, string fieldName)
		{
			string href = GetHref(asset, fieldName);
			if (string.IsNullOrEmpty(href) == false)
				href = "~" + href;
			else
				href = null;

			Log.Debug("GetNavigateUrl: returning '{0}'.", href);
			return href;
		}

		/// <summary>
		/// Gets the target asset when the link type is internal.  Will return a not-loaded Asset if the link is not internal.
		/// </summary>
		/// <param name="panel"></param>
		/// <param name="fieldName">The fieldname specified on InputHelper.ShowLink</param>
		/// <returns></returns>
		public static Asset GetTargetAsset(IFieldAccessor asset, string fieldName)
		{
			string assetPath = asset.Raw(fieldName + "_internal_link");
			Asset targetAsset = Asset.Load(assetPath);
			return targetAsset;
		}

		/// <summary>
		/// Gets the title specified for the link field.  
		/// If the link type is internal, then <see cref="GetTargetAsset"/> is called and passed to <see cref="OutputHelper.GetAssetTitle"/> to retrieve the title for the asset.
		/// Otherwise, returns the title specified in the given field.
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="fieldName">The fieldname specified on InputHelper.ShowLink</param>
		/// <returns></returns>
		public static string GetTitle(IFieldAccessor asset, string fieldName)
		{
			string linkType = asset.Raw(fieldName + "_type"); Log.Debug("linktype='{0}'", linkType);
			string title = asset[fieldName + "_" + linkType + "_title"]; Log.Debug("title='{0}'", title);

			switch (linkType)
			{
				case "internal":
					if (String.IsNullOrWhiteSpace(title))
					{
						Asset targetAsset = GetTargetAsset(asset, fieldName);
						title = targetAsset.Label;
					}
					break;
				case "external":
					break;
				case "title only":
					title = asset[fieldName + "_titleonly_title"];
					break;
			}

			Log.Debug("GetTitle: returning {0}.", title);
			return title;
		}

		#endregion
	}

	public /*interface*/class IFieldAccessor /*would be applied to CMSAPI.Asset CMSAPI.PanelEntry and possibly others */
	{

		public IFieldAccessor(Asset source)
		{
			_type = 1;
			_asset = source;
		}
		public IFieldAccessor(PanelEntry panel)
		{
			_type = 2;
			_panel = panel;
		}
		public IFieldAccessor(PanelEntry panel, Asset parent)
		{
			//Need a way to reference the parent Asset of a Panel without needing to always pass both to a function.
			//Its annoying because there is no better API functionality when dealing with Assets and their children Panels.
			_type = 2;
			_panel = panel;
			_asset = parent;
		}
		//public IFieldAccessor(Raw raw)
		//{
		//    _type = 3;
		//    _raw = raw;
		//}

		public static implicit operator IFieldAccessor(Asset source)
		{
			return new IFieldAccessor(source);
		}
		public static implicit operator IFieldAccessor(PanelEntry source)
		{
			return new IFieldAccessor(source);
		}

		private int _type = 0;
		private Asset _asset;
		private PanelEntry _panel;
		//private Raw _raw;
		internal string this[string key]
		{
			get
			{
				switch (_type)
				{
					case 1: return _asset[key];
					case 2: return _panel[key];
					//case 3: return _raw[key];
				}
				return null;
			}
		}
		internal string Raw(string key)
		{
			switch (_type)
			{
				case 1: return _asset.Raw[key];
				case 2: return _panel.Raw[key];
				//case 3: return _raw[key];
			}
			return null;
		}

		public List<PanelEntry> GetPanels(string key)
		{
			switch (_type)
			{
				case 1: return _asset.GetPanels(key);
				case 2: return _panel.GetPanels(key);
			}

			return new List<PanelEntry>();
		}

		public UploadedFiles GetUploadedFiles()
		{
			switch (_type)
			{
				case 1: return _asset.UploadedFiles;
				case 2: return _panel.UploadedFiles;
			}

			return null;
		}

		public string GetFieldName(string key)
		{
			switch (_type)
			{
				case 2: return _panel.GetFieldName(key);
				default: return key;
			}
		}

		public Asset GetAsset()
		{
			return _asset;
		}

	}

	public enum SortDirection
	{
		Unspecified = 0,
		Ascending = 1,
		Descending = -1
	}

	public class AssetPathResolver
	{

		public AssetPathStyle AssetsRootPathStyle { get; set; }

		private AssetPathResolver()
		{
			AssetsRootPathStyle = AssetPathStyle.OrganizationLanguage;
		}

		private static AssetPathResolver S_current = new AssetPathResolver();
		public static AssetPathResolver Current
		{
			get { return S_current; }
		}

		private Dictionary<string, string> _specialPaths = new Dictionary<string, string>();
		public IDictionary<string, string> SpecialPaths
		{
			get { return this._specialPaths; }
		}

		public string ResolvePath(Asset current, string assetPath)
		{
			List<string> assetPath2 = new List<string>(assetPath.Split('/'));

			if (assetPath2[0] == "$")
			{
				switch (AssetsRootPathStyle)
				{
					case AssetPathStyle.OrganizationLanguage:
						assetPath2[0] = current.AssetPath[0];
						assetPath2.Insert(1, current.AssetPath[1]);
						break;
				}
			}

			//check for special paths in the specialPaths structure.
			foreach (string key in SpecialPaths.Keys)
			{
				if (string.Compare(assetPath2[0], key, true) == 0)
				{
					List<string> assetPath3 = new List<string>(SpecialPaths[key].Split('/'));

					//replace first
					assetPath2[0] = GetPathPart(assetPath3[1], current.AssetPath);

					//insert all the rest
					for (int index = 2; index < assetPath3.Count; index++)
					{
						assetPath2.Insert(index - 1, GetPathPart(assetPath3[index], current.AssetPath));
					}
				}
			}

			if (assetPath2[0] == "." || assetPath2[0] == "..")
			{
				//remember and remove the relDir specifier
				string relDir = assetPath2[0];
				assetPath2.RemoveAt(0);

				int index = 0;
				foreach (var part in current.AssetPath.Take(current.AssetPath.Count - relDir.Length))
				{
					assetPath2.Insert(index++, part);
				}
			}

			return string.Join("/", assetPath2);
		}

		private static string GetPathPart(string part, AssetPath current)
		{
			int indexOfBrace = part.IndexOf("{");
			if (indexOfBrace == -1) return part;

			char[] pathChars = part.ToCharArray();
			int braceIndexerLength = 0;
			for (int i = indexOfBrace + 1; i < pathChars.Length; i++)
			{
				if ((pathChars[i] >= '0' && pathChars[i] <= '9') == false) break;
				braceIndexerLength++;
			}
			string pathIndexStr = part.Substring(indexOfBrace + 1, braceIndexerLength);

			int pathIndex = int.Parse(pathIndexStr);
			return current[pathIndex];
		}

	}

	public enum AssetPathStyle
	{
		/// <summary>
		/// a folder structure where the site's content starts in the root of the CMS instance.  Similar to: /Homepage, /Some content, /About Us/Information
		/// </summary>
		SiteRoot = 1
		,
		/// <summary>
		/// a folder structure where the content is loaded into an Org Unit, with no language codes.  Similar to: /AdventGeneral, /Simple Site, /CompanyName, /CompanyName-West
		/// </summary>
		Organization = 2
		,
		/// <summary>
		/// a folder structure where the content is loaded into an Org Unit first and Language after, then any assets.  Similar to: /[Organization]/[Language]/ or /AssetPath[0]/AssetPath[1]
		/// </summary>
		OrganizationLanguage = 3
	}


}


