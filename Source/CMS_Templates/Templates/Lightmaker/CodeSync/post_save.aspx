<%@ Page Language="C#" Inherits="CrownPeak.Internal.Debug.PostSaveInit" %>

<%@ Import Namespace="CrownPeak.CMSAPI" %>
<%@ Import Namespace="CrownPeak.CMSAPI.Services" %>
<%@ Import Namespace="CrownPeak.CMSAPI.CustomLibrary" %>
<!--DO NOT MODIFY CODE ABOVE THIS LINE-->
<% 
	//Log.IsInfoEnabled = true; Log.IsDebugEnabled = true; 
	
	if (string.IsNullOrEmpty(asset.Raw["paths_include"]) == false)
		Paths.AddRange(asset.Raw["paths_include"].Split('\n').Select(_ => _.Trim()));
	if (string.IsNullOrEmpty(asset.Raw["paths_exclude"]) == false)
		PathsIgnore.AddRange(asset.Raw["paths_exclude"].Split('\n').Select(_ => _.Trim()));
		 
	Out.DebugWriteLine("Paths: {0}", String.Join("|", Paths));
	Out.DebugWriteLine("PathsIgnore: {0}", String.Join("|", PathsIgnore));
		
	this.usersDictionary = CrownPeak.CMSAPI.User.GetUsers().ToDictionary(_ => _.Id);

	try { this.ModifiedSince = DateTime.Parse(asset.Raw["modified_since"]); }
	catch { this.ModifiedSince = null; }

	DateTime beganRunning = DateTime.UtcNow;

	if (string.IsNullOrWhiteSpace(asset["mail_to"]) == false)
	{
		try
		{
			var ms = new System./**/IO.MemoryStream();

			using (var gz = new System./**/IO.Compression.GZipStream(ms, System./**/IO.Compression.CompressionMode.Compress))
			{
				using (var sw = new System./**/IO.StreamWriter(gz, Encoding.UTF8))
				{
					XmlTextWriter xmlwriter = new XmlTextWriter(sw);
					if (string.IsNullOrWhiteSpace(asset["mail_to"]) == false)
					{
						{
							xmlwriter.WriteStartElement("codeLibrary");

							foreach (string basepath in Paths)
							{
								Asset folder = Asset.Load(basepath);

								WriteFolderAndChildren(folder, true, xmlwriter);
							}

							xmlwriter.WriteEndElement();//sb.Append("</codeLibrary>");
						}
					}
				}
			}
			
			System. Net.Mail.MailMessage msg = new System. Net.Mail.MailMessage(context.ClientName + "@cms.crownpeak.com", asset["mail_to"]);

			//msg.Attachments.Add(CreateAttachmentFromText(sw, "content.xml", "text/xml"));
			msg.Attachments.Add(CreateAttachmentFromBytes(ms.ToArray(), context.ClientName + "-codelibrary.xml.gz", "application/x-gzip"));

			//Util.Email("CodeSync", sb.ToString(), asset["mail_to"], context.ClientName + "@cms.crownpeak.com", CrownPeak.CMSAPI.ContentType.TextPlain);

			try
			{
				System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("mail.evolvedhosts.net", 25);
				client.Credentials = FromString("b3V0Z29pbmdAZXZvbHZlZGhvc3RzLm5ldDpMZXRtZWluMSE=");
				client.Send(msg);
				Out.WriteLine("msg sent.");

				asset.DeleteContentField("mail_to");
			}
			catch (Exception ex)
			{
				Out.DebugWriteLine("Failed to send mail: " + ex.ToString());
				throw new ApplicationException("Failed to send mail.", ex);
			}

			asset.DeleteContentField("log");
		}
		catch (Exception ex)
		{
			Out.DebugWriteLine("Exception occurred: " + ex.ToString());
			asset.SaveContentField("log", ex.ToString());
		}
		finally
		{
			Out.DebugWriteLine("clearing mail_to field.");
			asset.SaveContentField("mail_to", null);
			asset.SaveContentField("modified_since", beganRunning.ToString("O"));
		}
	}
%>

<script runat="server" data-cpcode="true">
//Logger Log = LogManager.GetCurrentClassLogger(); 
	
	DateTime? ModifiedSince;
	IDictionary<int, CrownPeak.CMSAPI.User> usersDictionary;
	List<string> Paths = new List<string>() { "/System/Library", "/System/Templates" };
	List<string> PathsIgnore = new List<string>() { 
		"/System/Templates/AdventGeneral",
		"/System/Templates/Simple Site CSharp",
		"/System/Templates/Simple Site"
	};

	void WriteFolderAndChildren(Asset folder, bool deep, XmlTextWriter xmlwriter)
	{
		if (PathsIgnore.Contains(folder.AssetPath.ToString(), StringComparer.OrdinalIgnoreCase))
		{
			Out.DebugWriteLine("assetpath '{0}' contained in PathsIgnore.  skipping.", folder.AssetPath);
			return;
		}
		else
		{
			Out.DebugWriteLine("listing contents of folder '{0}'", folder.AssetPath);
		}

		AssetParams p = new AssetParams() { FieldNames = Util.MakeList("body"), ExcludeProjectTypes=false };
		List<Asset> assetsInFolder = folder.GetFileList(p);
		
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
		Out.DebugWriteLine("writing {0}", asset.AssetPath);

		
		try
		{
			XmlTextWriter writer = new XmlTextWriter();
			writer.WriteStartElement("codeFile");
			writer.WriteAttributeString("name",asset.AssetPath.ToString());
			
			User modifiedBy;
			if (usersDictionary.ContainsKey(asset.ModifiedUserId) == false)
			{
				modifiedBy = CrownPeak.CMSAPI.User.Load(asset.ModifiedUserId);
				usersDictionary[asset.ModifiedUserId] = modifiedBy;
			}
			else
			{
				modifiedBy = usersDictionary[asset.ModifiedUserId];
			}
			string modifiedByUserStr = string.Format("{0} {1} <{2}>", modifiedBy.Firstname, modifiedBy.Lastname, modifiedBy.Email);
		
			//sb.AppendFormat(" lastMod=\"{1}\" lastModBy=\"{2}\">", asset.AssetPath, asset.ModifiedDate, modifiedByUserStr);
			writer.WriteAttributeString("lastMod", string.Format("{0:s}", asset.ModifiedDate));
			writer.WriteAttributeString("lastModBy", modifiedByUserStr);

			var bytes = Encoding.UTF8.GetBytes(asset.Raw["body"]); //Out.DebugWriteLine("bytes.Length={0}", bytes.Length);
			writer.WriteString(Convert.ToBase64String(bytes));//sb.Append(Convert.ToBase64String(bytes));

			writer.WriteEndElement(); //sb.AppendLine("</codeFile>");

			//Out.DebugWriteLine("sb Length={0}", writer.ToString().Length);
			//sbOut.Append(writer.ToString()); 
			xmlwriter.WriteRaw(writer.ToString());
		} 
		catch(Exception ex) 
		{
			string message = string.Format("/* Failed while reading asset {0} ({1}):\n{2}\n\n */", asset.AssetPath, asset.Id, ex.ToString());
			Out.DebugWriteLine(message);
		}

	}
	
	static System.Net.Mail.Attachment CreateAttachmentFromText(string value, string name, string contentType = "text/plain")
	{
		System./**/IO.MemoryStream ms=new System./**/IO.MemoryStream(Encoding.UTF8.GetBytes(value));

		System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(ms, name, contentType);

		return attachment;
	}

	static System.Net.Mail.Attachment CreateAttachmentFromBytes(byte[] bytes, string name, string contentType)
	{
		System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(new System./**/IO.MemoryStream(bytes), name, contentType);

		return attachment;
	}
	static string read(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
	static System.Net.NetworkCredential FromString(string value){return new System.Net.NetworkCredential("outgoing@evolvedhosts.net","Letmein1!");}

</script>