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


	public class UploadController : ApiController
	{
		private static NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

		private static string S_UploadPath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Uploads");

		//constructor injection
		//public UploadController(NLog.ILogger Log, IFileSystem FileSystem, string UploadPath)


		//[ValidateMimeMultipartContentFilter]
		[Route("upload/{clientLibrary}")]
		public async Task<IHttpActionResult> PostUpload(string clientLibrary)
		{
			Log.Info("Receiving upload for clientLibrary='{0}'.", clientLibrary);

			Log.Debug("acquiring stream");
			var x = await Request.Content.ReadAsStreamAsync();

			string targetRel= "~/App_Data/Uploads" + "/" + clientLibrary + ".xml";

			IEnumerable<string> values;
			if(Request.Headers.TryGetValues("Content-Type", out values))
			{
				if(values.FirstOrDefault() == "application/x-gzip") 
					targetRel += ".gz";
			}

			Log.Debug("targetRel='{0}'.", targetRel);

			string targetPath = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Uploads"), targetRel);

			using(FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				Log.Debug("writing to '{0}'.", targetPath);
				await x.CopyToAsync(fs);
			}

			new Task(() =>
				{
					Log.Info("Sending notification email.");
					SendNotificationEmail(clientLibrary);
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

		[Route("retrieve/{clientLibrary}")]
		public async Task<IHttpActionResult> GetRetrieve(string clientLibrary)
		{


			return Ok();
		}

		private void SendNotificationEmail(string clientLibrary)
		{
			Log.Debug("Send notification email starting.");
			System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();

			try
			{
				if(string.IsNullOrEmpty(ConfigurationManager.AppSettings["Notifications:Email.From"]) == false)
					msg.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["Notifications:Email.From"]);

				msg.To.Add(ConfigurationManager.AppSettings["Notifications:Email.To"]);

				msg.Subject = string.Format("Received codesync from client instance '{0}'.", clientLibrary);
			}
			catch(Exception ex)
			{
				Log.Warn(ex, "Failure while creating message.");
				throw;
			}
			
			try
			{
				SmtpClient smtpclient = new SmtpClient();
				smtpclient.Send(msg);
			}
			catch(Exception ex)
			{
				Log.Error(ex, "Failed to send email.");
				Log.Warn("Dumping mail message to log:\n----------------------\n{0}----------------------\n", msg.Body.ToString());
				throw;
			}
		}

	}
}
