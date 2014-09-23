﻿<%@ Page Language="C#" Inherits="CrownPeak.Internal.Debug.PostSaveInit" %>

<%@ Import Namespace="CrownPeak.CMSAPI" %>
<%@ Import Namespace="CrownPeak.CMSAPI.Services" %>
<%@ Import Namespace="CrownPeak.CMSAPI.CustomLibrary" %>
<!--DO NOT MODIFY CODE ABOVE THIS LINE-->
<% 
	//Log.IsInfoEnabled = true; Log.IsDebugEnabled = true; 

	if (string.IsNullOrEmpty(asset.Raw["paths_include"]) == false)
		Paths = asset.Raw["paths_include"].Split('\n').Select(_ => _.Trim()).ToArray();
	if (string.IsNullOrEmpty(asset.Raw["paths_exclude"]) == false)
		PathsIgnore = asset.Raw["paths_exclude"].Split('\n').Select(_ => _.Trim()).ToArray();

	this.usersDictionary = CrownPeak.CMSAPI.User.GetUsers().ToDictionary(_ => _.Id);

	try { this.ModifiedSince = DateTime.Parse(asset.Raw["modified_since"]); }
	catch { this.ModifiedSince = null; }

	DateTime beganRunning = DateTime.UtcNow;

	try
	{
		System. IO.MemoryStream ms = new System. IO.MemoryStream();
		if (string.IsNullOrWhiteSpace(asset["mail_to"]) == false)
		{
			using (System. IO.Compression.GZipStream gz = new System. IO.Compression.GZipStream(ms, System. IO.Compression.CompressionMode.Compress))
			{
				System. IO.StreamWriter sw = new System. IO.StreamWriter(gz, Encoding.UTF8);

				sw.Write("<codeLibrary>\n");

				foreach (string basepath in Paths)
				{
					Out.DebugWriteLine("beginning with path: {0}", basepath);
					Asset folder = Asset.Load(basepath);

					WriteFolderAndChildren(folder, true, sw);
				}

				sw.Write("</codeLibrary>");
				sw.Flush();

				gz.Flush();
			}
		}

		if (string.IsNullOrWhiteSpace(asset["mail_to"]) == false)
		{
			System. Net.Mail.MailMessage msg = new System. Net.Mail.MailMessage(context.ClientName + "@cms.crownpeak.com", asset["mail_to"]);

			//msg.Attachments.Add(CreateAttachmentFromText(sw, "content.xml", "text/xml"));
			msg.Attachments.Add(CreateAttachmentFromBytes(ms.ToArray(), context.ClientName + "-codelibrary.xml.gz", "application/x-gzip"));

			//Util.Email("CodeSync", sb.ToString(), asset["mail_to"], context.ClientName + "@cms.crownpeak.com", CrownPeak.CMSAPI.ContentType.TextPlain);

			try
			{
				System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("mail.evolvedhosts.net", 25);
				client.Credentials = new System.Net.NetworkCredential("outgoing@evolvedhosts.net", "Letmein1!");
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
%>

<script runat="server" data-cpcode="true">#line 82
	//Logger Log = LogManager.GetCurrentClassLogger(); 

	DateTime? ModifiedSince;
	IDictionary<int, CrownPeak.CMSAPI.User> usersDictionary;
	string[] Paths = new string[] { "/System/Library", "/System/Templates" };
	string[] PathsIgnore = new string[] { "/System/Templates/AdventGeneral", "/System/Templates/SimpleSiteCSharp", "/System" };

	void WriteFolderAndChildren(Asset folder, bool deep, System. IO.TextWriter sb)
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

		List<Asset> assetsInFolder = folder.GetFileList();
		foreach (var asset1 in assetsInFolder)
		{
			if (asset1.ModifiedDate > ModifiedSince.GetValueOrDefault())
			{
				WriteFileNode(asset1, sb);
			}
			else
			{
				Out.DebugWriteLine("Skipping asset '{0}' ({1}) since modified date '{2}' < modified_since", asset1.Label, asset1.Id, asset1.ModifiedDate);
			}
		}

		if (deep)
		{
			foreach (Asset folder2 in folder.GetFolderList())
			{
				WriteFolderAndChildren(folder2, deep, sb);
			}
		}
	}

	void WriteFileNode(Asset asset, System. IO.TextWriter sb)
	{
		Out.DebugWriteLine("writing {0}", asset.AssetPath);

		sb.Write("<codeFile");

		try
		{
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
			string modifiedByUserStr = Util.HtmlEncode(string.Format("{0} {1} <{2}>", modifiedBy.Firstname, modifiedBy.Lastname, modifiedBy.Email));

			sb.Write(" name=\"{0}\" lastMod=\"{1}\" lastModBy=\"{2}\">", asset.AssetPath, asset.ModifiedDate, modifiedByUserStr);

			//System
			//    .IO
			//    .MemoryStream memstream = new System
			//        .IO
			//        .MemoryStream();

			//using (System
			//    .IO
			//    .Compression.GZipStream gzip = new System
			//        .IO
			//        .Compression.GZipStream(memstream, System
			//            .IO.Compression.CompressionMode.Compress))
			//{
			//    var bytes = Encoding.UTF8.GetBytes(asset["body"]);
			//    gzip.Write(bytes, 0, bytes.Length);

			//    //sb.AppendFormat("[[{0}]]", bytes.Length);
			//}
			//sb.Append(Convert.ToBase64String(memstream.ToArray()));

			var bytes = Encoding.UTF8.GetBytes(asset["body"]);
			sb.Write(Convert.ToBase64String(bytes));


		}
		catch (Exception ex)
		{
			sb.Write(">"); //finish the codeFile node
			string message = string.Format("/* Failed while reading asset {0} ({1}):\n{2}\n\n */", asset.AssetPath, asset.Id, ex.ToString());
			sb.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));
		}
		sb.WriteLine("</codeFile>");
	}

	static System
		.Net
		.Mail.Attachment CreateAttachmentFromText(string value, string name, string contentType = "text/plain")
	{
		System
			.IO.MemoryStream ms = new System
				.IO.MemoryStream(Encoding.UTF8.GetBytes(value));

		System
			.Net.Mail.Attachment attachment = new System.
				Net.Mail.Attachment(ms, name, contentType);

		return attachment;
	}

	static System
		.Net
		.Mail.Attachment CreateAttachmentFromBytes(byte[] bytes, string name, string contentType)
	{

		System
			.Net.Mail.Attachment attachment = new System.
				Net.Mail.Attachment(new System. IO.MemoryStream(bytes), name, contentType);

		return attachment;
	}

</script>
