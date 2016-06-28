<%@ Page Language="C#" Inherits="CrownPeak.Internal.Debug.PostSaveInit" %>
<%@ Import Namespace="CrownPeak.CMSAPI" %>
<%@ Import Namespace="CrownPeak.CMSAPI.Services" %>
<%@ Import Namespace="CrownPeak.CMSAPI.CustomLibrary" %>
<%@ Import Namespace="LMCP" Package="Lm.Core,1.0.0" %>
<!--DO NOT MODIFY CODE ABOVE THIS LINE-->
<% 
	Log = new EmailLogger("CodeSync " + context.ClientName, recipients:"eric.newton@lightmaker.com");
	Log.IsDebugEnabled = asset.Raw["IsDebugEnabled"]=="true";
	
	//Log.IsInfoEnabled = true; Log.IsDebugEnabled = true; 
	asset.DeleteContentField("log");

	if(asset["skip_package_sources"] == "true")
		skipFilesUnderscore = true;
	
	if (string.IsNullOrEmpty(asset.Raw["paths_include"]) == false)
		Paths.AddRange(asset.Raw["paths_include"].Split('\n').Select(_ => _.Trim()));
	if (string.IsNullOrEmpty(asset.Raw["paths_exclude"]) == false)
		PathsIgnore.AddRange(asset.Raw["paths_exclude"].Split('\n').Select(_ => _.Trim()));
		 
	Log.Debug("Paths: {0}", String.Join("|", Paths));
	Log.Debug("PathsIgnore: {0}", String.Join("|", PathsIgnore));
		
	this.usersDictionary = CrownPeak.CMSAPI.User.GetUsers().ToDictionary(_ => _.Id);

	try { this.ModifiedSince = DateTime.Parse(asset.Raw["modified_since"]); }
	catch { this.ModifiedSince = null; }

	DateTime beganRunning = DateTime.UtcNow;

	Log.Debug("starting");
	
	if (string.IsNullOrWhiteSpace(asset["mail_to"]) == false)
	{
		try
		{
			var orig = new System./**/IO.MemoryStream();

			using(var gz = new System./**/IO.Compression.GZipStream(orig, System./**/IO.Compression.CompressionMode.Compress))
			{

				using(var sw = new System./**/IO.StreamWriter(gz, Encoding.UTF8))
				{
					XmlTextWriter xmlwriter = new XmlTextWriter(sw);
					{
						xmlwriter.WriteStartElement("CodeLibrary");

						foreach(string basepath in Paths)
						{
							Asset folder = Asset.Load(basepath);

							WriteFolderAndChildren(folder, true, xmlwriter);
						}

						xmlwriter.WriteEndElement();//sb.Append("</codeLibrary>");
					}

					//asset.SaveContentField("CodeLibrary", target.ToString());
				}
			}

			//byte[] bytes = Encoding.UTF8.GetBytes("<test><node1>Test</node1></test>");
			//ms = new System./**/IO.MemoryStream(bytes);
			
			//System. Net.Mail.MailMessage msg = new System. Net.Mail.MailMessage(context.ClientName + "@cms.crownpeak.com", asset["mail_to"]);

			//msg.Attachments.Add(CreateAttachmentFromText(sw, "content.xml", "text/xml"));
			//msg.Attachments.Add(CreateAttachmentFromBytes(ms.ToArray(), context.ClientName + "-codelibrary.xml.gz", "application/x-gzip"));
			//var attachment1 = CreateAttachmentFromBytes(orig.ToArray(), context.ClientName + "-codelibrary.xml.gz", "application/x-gzip");

			Log.Info("attachment created.");

			Asset.CreateFromBase64("CodeLibrary.xml", asset.Parent, Convert.ToBase64String(orig.ToArray()));
			
			//Util.Email("CodeSync", sb.ToString(), asset["mail_to"], context.ClientName + "@cms.crownpeak.com", CrownPeak.CMSAPI.ContentType.TextPlain);
			//PostHttpParams postHttpParams = this.CreateMultipartFormUpload(attachment1);

			Log.Info("posting http");
			//Util.PostHttp("http://cputil.lightmakerusa.com/codesync/api/upload/library", postHttpParams);

			var wc = new System./**/Net.WebClient();
			wc.UploadData("http://cputil.lightmakerusa.com/codesync/api/upload/library/"+context.ClientName, "POST", orig.ToArray());
					
			//System./**/Net.WebRequest req = System./**/Net.HttpWebRequest.Create("http://cputil.lightmakerusa.com/codesync/api/upload/library");
			//req.Method = "POST";
			//foreach(var h in postHttpParams.Headers) 
			//{
			//	if(h.StartsWith("Content-Type", StringComparison.OrdinalIgnoreCase) || h.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
			//	req.ContentLength = postHttpParams.PostData.Length;
			//	req.Headers.Add(h);
			//}

			//using(var resp = req.GetResponse())
			//{
			//	Log.Debug(string.Format("resp.contentlength={0}", resp.ContentLength));
			//}
			
				
			Log.Info("msg sent.");

			asset.DeleteContentField("mail_to");
			
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Exception occurred");
		}
		finally
		{
			Log.Debug("clearing mail_to field.");
			asset.SaveContentField("mail_to", null);
			asset.SaveContentField("modified_since", beganRunning.ToString("O"));
			
			asset.SaveContentField("log", Log.GetLog());
			Log.Flush();
		}
	}
%>

<script runat="server" data-cpcode="true">
//Logger Log = LogManager.GetCurrentClassLogger(); 
	EmailLogger Log;
	
	DateTime? ModifiedSince;
	IDictionary<int, CrownPeak.CMSAPI.User> usersDictionary;
	List<string> Paths = new List<string>() { "/System/Library", "/System/Templates" };
	List<string> PathsIgnore = new List<string>() { 
		"/System/Templates/AdventGeneral",
		"/System/Templates/Simple Site CSharp",
		"/System/Templates/Simple Site"
	};

	bool skipFoldersUnderscore = true;
	bool skipFilesUnderscore = false;
	

	void WriteFolderAndChildren(Asset folder, bool deep, XmlTextWriter xmlwriter)
	{
		Log.Info("Folder '{0}'.", folder.AssetPath);

		//evaluate skip rules
		//if(skipFoldersUnderscore && folder.AssetPath.Last().StartsWith("_"))
		//{
		//	Log.Info("skipping because of underscore: '{0}'", folder.AssetPath.Last());
		//	return;
		//}
		if(PathsIgnore.Contains(folder.AssetPath.ToString(), StringComparer.OrdinalIgnoreCase))
		{
			Log.Info("skipping because of PathsIgnore", folder.AssetPath);
			return;
		}

		AssetParams p = new AssetParams() { FieldNames = Util.MakeList("body"), ExcludeProjectTypes=false };
		List<Asset> assetsInFolder = folder.GetFileList(p).OrderBy(_ => _.Label).ToList();
		
		Log.Debug("Found {0} assets in folder '{1}'.", assetsInFolder.Count, folder.AssetPath);
		
		foreach (var asset1 in assetsInFolder)
		{
			bool skip = false;

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
			if (skip == false) WriteFileNode(asset1, xmlwriter);
		}

		if (deep)
		{
			foreach (Asset folder2 in folder.GetFolderList())
			{
				WriteFolderAndChildren(folder2, deep, xmlwriter);
			}
		}
	}
	
	bool CheckForIgnored(Asset asset1)
	{
		string assetpathstr = asset1.AssetPath.ToString().Replace("/" + asset1.Label, "");
		for(int i=0; i < PathsIgnore.Count(); i++)
		{
			if (assetpathstr.StartsWith(PathsIgnore[i], StringComparison.InvariantCultureIgnoreCase))
				return true;
		}
		return false;
	}

	void WriteFileNode(Asset asset, XmlTextWriter xmlwriter) 
	{
		if(asset == null) throw new ArgumentNullException("asset");
		
		Log.Debug("File: {0}", asset.AssetPath);
		
		//evaluate skip rules
		if(skipFilesUnderscore && asset.AssetPath.Last().StartsWith("_"))
		{
			Log.Debug("skipping because of underscore '{0}'", asset.AssetPath.Last());
			return;
		}
		
		try
		{
			XmlTextWriter writer = new XmlTextWriter();
			writer.WriteStartElement("CodeFile");
			writer.WriteAttributeString("Name",asset.AssetPath.ToString());
			writer.WriteAttributeString("Encoding", "base64");
			//Log.Debug("1");

			string modifiedByUserStr = GetModifiedByUserInfo(asset);
			//Log.Debug("2");
			
			//sb.AppendFormat(" lastMod=\"{1}\" lastModBy=\"{2}\">", asset.AssetPath, asset.ModifiedDate, modifiedByUserStr);
			writer.WriteAttributeString("LastMod", string.Format("{0:s}", asset.ModifiedDate));
			writer.WriteAttributeString("LastModBy", modifiedByUserStr);

			var bytes = Encoding.UTF8.GetBytes(asset.Raw["body"]); //Out.DebugWriteLine("bytes.Length={0}", bytes.Length);
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
		if(usersDictionary.ContainsKey(asset.ModifiedUserId) == false)
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
			modifiedByUserStr = string.Format("{0} {1} <{2}>", modifiedBy.Firstname, modifiedBy.Lastname, modifiedBy.Email);
		}
		else
		{
			Log.Debug("[user {0} not found on asset {1}]", asset.ModifiedUserId, asset.Id);
			modifiedByUserStr = "";
		}

		return modifiedByUserStr;
	}
	
	

</script>