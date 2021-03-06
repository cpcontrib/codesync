﻿<%@ Page Language="C#" Inherits="CrownPeak.Internal.Debug.PostSaveInit" %>

<%@ Import Namespace="CrownPeak.CMSAPI" %>
<%@ Import Namespace="CrownPeak.CMSAPI.Services" %>
<%@ Import Namespace="CrownPeak.CMSAPI.CustomLibrary" %>
<% //@Package=Lm.Core,1.0.0 %>
<!--DO NOT MODIFY CODE ABOVE THIS LINE-->
<% 
	Initialize(LogDebug: false);

	//Log.IsInfoEnabled = true; Log.IsDebugEnabled = true; 
	asset.DeleteContentField("log");

	if(asset["skip_package_sources"] == "true")
		skipFilesUnderscore = true;

	if(string.IsNullOrEmpty(asset.Raw["paths_include"]) == false)
	{
		Paths.Clear();
		Paths.AddRange(asset.Raw["paths_include"].Split('\n').Select(_ => _.Trim()));
	}
	if(string.IsNullOrEmpty(asset.Raw["paths_exclude"]) == false)
	{
		PathsIgnore.Clear();
		PathsIgnore.AddRange(asset.Raw["paths_exclude"].Split('\n').Select(_ => _.Trim())); //Globber.RegexGlob(_.Trim())));
	}

	Log.Info("Paths: {0}", String.Join("|", Paths));
	Log.Info("PathsIgnore: {0}", String.Join("|", PathsIgnore));

	this.usersDictionary = CrownPeak.CMSAPI.User.GetUsers().ToDictionary(_ => _.Id);

	try { this.ModifiedSince = DateTime.Parse(asset.Raw["modified_since"]); }
	catch { this.ModifiedSince = null; }

	DateTime beganRunning = DateTime.UtcNow;

	Log.Info("starting");
	

	try
	{

		XmlTextWriter xmlwriter = new XmlTextWriter(indented: true);
		{
			xmlwriter.WriteStartElement("CodeLibrary");

			WriteFolderAndChildren(xmlwriter);
			WriteUserReferences(usersDictionary.Values, xmlwriter);

			xmlwriter.WriteEndElement();
		}

		Log.Info("attachment created.");

		UploadToServer(xmlwriter);

		asset.SaveContentField("_last_result", string.Format("Operation completed at {0}", beganRunning.ToString("O")));
	}
	catch(Exception ex)
	{
		Log.Error(ex, "Exception occurred");

		asset.SaveContentField("_last_result", string.Format("Operation failed: {0}", ex.ToString()));
	}
	finally
	{
		Log.Info("Completing process.");

		Log.Debug("clearing mail_to field.");
		asset.SaveContentField("mail_to", null);
		asset.SaveContentField("modified_since", beganRunning.ToString("O"));

		asset.SaveContentField("log", Log.GetLog());
		Log.Flush();
	}
%>

<script runat="server" data-cpcode="true">

	EmailLogger Log;

	string _CodeSyncBaseUri;
	DateTime? ModifiedSince;
	IDictionary<int, CrownPeak.CMSAPI.User> usersDictionary;
	List<string> Paths = new List<string>() { "/System/Library", "/System/Templates" };
	List<string> PathsIgnore = new List<string>();
	// { 
	//	"/System/Templates/AdventGeneral",
	//	"/System/Templates/Simple Site CSharp",
	//	"/System/Templates/Simple Site"
	//};


	public void Initialize(bool LogDebug = false)//would be the ctor
	{
		_CodeSyncBaseUri = "https://codesync.cp-contrib.com";

		Log = new EmailLogger("CodeSync " + context.ClientName, recipients: "ericnewton76@gmail.com");
		if(LogDebug) Log.IsDebugEnabled = LogDebug;// asset.Raw["IsDebugEnabled"] == "true";
	}

	bool skipFoldersUnderscore = true;
	bool skipFilesUnderscore = false;

	void WriteFolderAndChildren(XmlTextWriter xmlwriter)
	{
		foreach(string basepath in Paths)
		{
			Asset folder = Asset.Load(basepath);

			WriteFolderAndChildren(folder, true, xmlwriter);
		}
	}

	private class AssetPathComparer : IComparer<AssetPath>
	{

		public int Compare(AssetPath x, AssetPath y)
		{
			return string.Compare(x.ToString(), y.ToString(), true);
		}
	}

	void WriteFolderAndChildren(Asset folder, bool deep, XmlTextWriter xmlwriter)
	{
		if(ProcessFolderNode(folder) == false) return;

		FilterParams p = new FilterParams() { FieldNames = Util.MakeList("body"), ExcludeProjectTypes = false };

		List<Asset> assetsInFolder = folder.GetFilterList(p).OrderBy(_ => _.Label).OrderBy(_ => _.AssetPath, new AssetPathComparer()).ToList();

		Log.Info("Found {0} assets in folder '{1}'.", assetsInFolder.Count, folder.AssetPath);

		foreach(var asset1 in assetsInFolder)
		{
			string msg = string.Format("File:{0}", asset1.AssetPath.ToString());
			try
			{
				bool skip = false;
				if(ProcessFileNode(asset1) == false) skip = true;

				//if (CheckForIgnored(asset1))//asset1.ModifiedDate > ModifiedSince.GetValueOrDefault())
				//{
				//    Out.DebugWriteLine("Ignoring asset '{0}'.", asset1.AssetPath.ToString());
				//    skip = true;
				//}
				//if(false)
				//{
				//    Out.DebugWriteLine("Skipping asset '{0}' ({1}) since modified date '{2}' < modified_since", asset1.Label, asset1.Id, asset1.ModifiedDate);
				//    skip = true;
				//}
				if(skip == false && asset1.IsFile == true)
					WriteFileNode(asset1, xmlwriter);

				if(skip && Log.IsDebugEnabled)
				{
					msg += string.Format(" skip={0}", skip);
					Log.Debug(msg);
				}
				else
					Log.Debug(msg);
			}
			catch(Exception ex)
			{
				Log.Error(ex, msg);
			}
		}

		if(false) //deep)
		{
			foreach(Asset folder2 in folder.GetFolderList())
			{
				WriteFolderAndChildren(folder2, deep, xmlwriter);
			}
		}
	}

	bool ProcessFolderNode(Asset folder)
	{
		return ProcessFolderNode(folder.AssetPath.ToString());
	}

	bool ProcessFolderNode(string folderAssetPath)
	{
		bool process = true; //default is to process it
		string msg = string.Format("Folder '{0}'.", folderAssetPath);

		//evaluate skip rules
		//if(skipFoldersUnderscore && folder.AssetPath.Last().StartsWith("_"))
		//{
		//	Log.Info("Folder '{0}': skipping because of underscore: '{1}'", folder.AssetPath, folder.AssetPath.Last());
		//	return;
		//}

		foreach(var PathIgnoreSpec in PathsIgnore)
		{
			if(folderAssetPath.StartsWith(PathIgnoreSpec))
			{
				msg += string.Format(": skipping because of PathsIgnore", folderAssetPath);
				process = false; break;
			}
			else
			{
				process = true;
			}

			Log.Info(msg);
		}

		return process;
	}

	bool ProcessFileNode(Asset asset)
	{
		bool process = true;
		string assetPathStr = asset.AssetPath.ToString();
		//string msg = string.Format("FileCheck:{0}", assetPathStr);

		//evaluate skip rules
		if(skipFilesUnderscore && asset.AssetPath.Last().StartsWith("_"))
		{
			//msg += string.Format(": skipping because of underscore '{0}'", asset.AssetPath.Last());
			process = false;
		}
		else
		{
			foreach(var ExcludePathSpec in this.PathsIgnore)
			{
				if(assetPathStr.StartsWith(ExcludePathSpec)) { process = false; break; }
			}
		}

		//Log.Info(msg);
		return process;
	}
	byte[] Sha1Hash(byte[] bytes)
	{
		using(var sha1 = new System.Security.Cryptography.SHA1Managed())
		{

			return sha1.ComputeHash(bytes);
		}
	}

	void WriteFileNode(Asset asset, XmlTextWriter xmlwriter)
	{
		if(asset == null) throw new ArgumentNullException("asset");

		if(ProcessFileNode(asset) == false) return;

		try
		{
			XmlTextWriter writer = new XmlTextWriter();
			writer.WriteStartElement("CodeFile");
			writer.WriteAttributeString("Name", asset.AssetPath.ToString());
			writer.WriteAttributeString("Encoding", "base64");
			//Log.Debug("1");

			string modifiedByUserStr = GetModifiedByUserInfo(asset);
			//Log.Debug("2");

			//sb.AppendFormat(" lastMod=\"{1}\" lastModBy=\"{2}\">", asset.AssetPath, asset.ModifiedDate, modifiedByUserStr);
			writer.WriteAttributeString("LastMod", (asset.ModifiedDate != null ? asset.ModifiedDate.Value.ToString("O") : ""));
			writer.WriteAttributeString("LastModBy", modifiedByUserStr);

			var bytes = Encoding.UTF8.GetBytes(asset.Raw["body"]); //Out.DebugWriteLine("bytes.Length={0}", bytes.Length);
			byte[] sha1hash = Sha1Hash(bytes);

			writer.WriteAttributeString("sha1Hash", BitConverter.ToString(sha1hash).Replace("-", ""));

			writer.WriteString(Convert.ToBase64String(bytes));//sb.Append(Convert.ToBase64String(bytes));

			writer.WriteEndElement(); //sb.AppendLine("</codeFile>");

			//Out.DebugWriteLine("sb Length={0}", writer.ToString().Length);
			//sbOut.Append(writer.ToString()); 

			//Log.Debug("3");

			string codefile = writer.ToString();
			xmlwriter.WriteRaw(codefile);
		}
		catch(Exception ex)
		{
			string message = string.Format("Failed while reading asset {0} ({1})", asset.AssetPath, asset.Id, ex.ToString());
			Log.Error(ex, message);
		}

	}

	private string GetModifiedByUserInfo(Asset asset)
	{
		User modifiedBy;
		if(usersDictionary.TryGetValue(asset.ModifiedUserId, out modifiedBy) == false)
		{
			modifiedBy = CrownPeak.CMSAPI.User.Load(asset.ModifiedUserId);
			usersDictionary[asset.ModifiedUserId] = modifiedBy;
		}
		else
		{
			modifiedBy = usersDictionary[asset.ModifiedUserId];
		}

		string modifiedByUserStr;
		if(modifiedBy != null)
		{
			modifiedByUserStr = modifiedBy.Id.ToString();
		}
		else
		{
			Log.Warn("[user {0} not found on asset {1}]", asset.ModifiedUserId, asset.Id);
			modifiedByUserStr = "";
		}

		return modifiedByUserStr;
	}

	private void WriteUserReferences(IEnumerable<User> users, XmlTextWriter xmlwriter)
	{
		xmlwriter.WriteStartElement("UserRefs");

		foreach(var user in users)
		{
			if(user==null) { Log.Debug("Encountered null user in users collection."); continue; }
			try
			{
				xmlwriter.WriteStartElement("u");
				xmlwriter.WriteAttributeString("id", user.Id.ToString());
				xmlwriter.WriteAttributeString("n", user.Firstname + " " + user.Lastname);
				xmlwriter.WriteAttributeString("e", user.Email);

				xmlwriter.WriteEndElement();
			}
			catch(Exception ex)
			{
				string msg = "";
				try { msg = "Failed to write a user: "; msg += user.Id.ToString(); }
				catch { }
				Log.Error(ex, msg);
			}
		}

		xmlwriter.WriteEndElement();
	}

	private void UploadToServer(XmlTextWriter xmlwriter)
	{
		var bytes = Encoding.UTF8.GetBytes(xmlwriter.ToString());

		//Asset.CreateFromBase64("CodeLibrary.xml", asset.Parent, Convert.ToBase64String(orig.ToArray()));

		//PostHttpParams postHttpParams = this.CreateMultipartFormUpload(attachment1);

		string uploadUrl = _CodeSyncBaseUri + "/api/v1/library/" + context.ClientName + "/upload";

		Log.Info("Posting data to '{0}'.", uploadUrl);
		//Util.PostHttp(CodeSyncBaseUri+"/api/upload/library/" + context.ClientName, postHttpParams);

		var wc = new WebClientEx() { Timeout = 30000 }; //30000 ms = 5 min max, CP plugin max timeout is 900 seconds

		//wc.Headers.Set("Content-Type", "application/x-gzip");
		wc.Headers.Set("Content-Type", "application/octet-stream");
		wc.UploadData(uploadUrl, "POST", bytes);

		Log.Info("Data uploaded to '{0}'.", uploadUrl);
	}

	private class WebClientEx : System./**/Net.WebClient
	{
		/// <summary>
		/// Timeout in milliseconds
		/// </summary>
		public int Timeout { get; set; }

		protected override System./**/Net.WebRequest GetWebRequest(Uri uri)
		{
			var lWebRequest = base.GetWebRequest(uri);
			lWebRequest.Timeout = Timeout;
			((System./**/Net.HttpWebRequest)lWebRequest).ReadWriteTimeout = Timeout;
			return lWebRequest;
		}
	}


</script>
