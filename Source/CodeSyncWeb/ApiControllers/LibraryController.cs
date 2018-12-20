using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Http;
using CodeSyncWeb.Components;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace CodeSyncWeb.ApiControllers
{
	[RoutePrefix("api/v1")]


	public class LibraryController : ApiController
	{
		private static NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

		private static string S_UploadPath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Uploads");

		//constructor injection
		//public UploadController(NLog.ILogger Log, IFileSystem FileSystem, string UploadPath)


		//[ValidateMimeMultipartContentFilter]
		[Route("library/{clientLibrary}/upload")]
		public async Task<IHttpActionResult> PostUpload(string clientLibrary)
		{
			Log.Info("Receiving upload for clientLibrary='{0}'.", clientLibrary);

			Log.Debug("acquiring stream");
			var x = await Request.Content.ReadAsStreamAsync();

			string targetRel= clientLibrary + ".xml.gz";

			bool incomingIsGzipped = false;

			IEnumerable<string> values;
			if(Request.Headers.TryGetValues("Content-Type", out values))
			{
				if(values.FirstOrDefault() == "application/x-gzip")
				{
					targetRel += ".gz";
					incomingIsGzipped = true;
				}

			}

			Log.Debug("targetRel='{0}'.", targetRel);
			Log.Debug("incomingIsGzipped={0}", incomingIsGzipped);

			string targetPath = Path.Combine(S_UploadPath, targetRel);

			using(FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				Stream target = fs;
				Log.Debug("writing to '{0}'.", targetPath);

				if(incomingIsGzipped == false)
				{
					target = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal);
				}

				try
				{
					await x.CopyToAsync(target);
				}
				finally
				{
					await target.FlushAsync();
					target.Dispose();
				}
			}

			//initiate async task to send notification that we received an upload
			new Task(() =>
				{
					try
					{
						Log.Info("Sending notification email.");
						SendNotificationEmail(clientLibrary);
					}
					catch(Exception ex)
					{
						Log.Error(ex, "Failed to send email.");
					}
				}).Start();

			Log.Debug("Returning accepted.");
			return StatusCode(HttpStatusCode.Accepted);
		}

		//[ValidateMimeMultipartContentFilter]
		[Route("content")]
		public async Task<IHttpActionResult> PostContent()
		{

			var streamProvider = new MultipartFormDataStreamProvider("");
			await Request.Content.ReadAsMultipartAsync(streamProvider);

			return StatusCode(HttpStatusCode.Accepted);
		}


		[Route("library/{clientLibrary}")]
		public async Task<IHttpActionResult> GetLibrary(string clientLibrary, bool refresh = false)
		{
			string libraryFile = clientLibrary + ".xml.gz";
			string libraryFilePath = Path.Combine(S_UploadPath, libraryFile);

			if(refresh || File.Exists(libraryFilePath) == false)
			{
				var success = codesynccore.RefreshLibrary(clientLibrary);
			}

			string outType = "application/gzip";

			foreach(var acceptType in Request.Headers.Accept)
			{
				if(acceptType.MediaType == "text/xml" || acceptType.MediaType == "text/plain")
				{
					outType = acceptType.MediaType;
				}
			}

			if(File.Exists(libraryFilePath) == false)
			{
				var respMsg = new HttpResponseMessage(HttpStatusCode.NotFound);
				respMsg.ReasonPhrase = string.Format("Client library not present.");
				return ResponseMessage(respMsg);
			}

			FileStream fs = new FileStream(libraryFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
			
			Stream output = fs;

			if(outType != "application/gzip")
			{
				output = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress);
			}

			var resp = this.Request.CreateResponse(HttpStatusCode.OK);
			var streamContent = new System.Net.Http.StreamContent(output);
			//await streamContent.ReadAsStreamAsync();
			resp.Content = streamContent;
			//resp.Headers.Add("Content-Type", outType);

			resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(outType);
			return ResponseMessage(resp);
		}

		private void SendNotificationEmail(string clientLibrary)
		{
			Log.Debug("Send notification email starting.");
			System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();


			try
			{
				try
				{
					if(string.IsNullOrEmpty(ConfigurationManager.AppSettings["Notifications:Email.From"]) == false)
						msg.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["Notifications:Email.From"]);

					msg.To.Add(ConfigurationManager.AppSettings["Notifications:Email.To"]);

					msg.Subject = string.Format("Received codesync from client instance '{0}'.", clientLibrary);
				}
				catch(Exception ex)
				{
					throw new ApplicationException("Failure while creating message.", ex);
				}

				try
				{
					SmtpClient smtpclient = new SmtpClient();
					smtpclient.Send(msg);
				}
				catch(Exception ex)
				{
					throw new ApplicationException("Failure while trying to send message.", ex);
				}
			}
			catch
			{
				Log.Warn("Dumping mail message to log:\n----------------------\n{0}----------------------\n", msg.Body.ToString());
				throw;
			}
		}

	}
}
