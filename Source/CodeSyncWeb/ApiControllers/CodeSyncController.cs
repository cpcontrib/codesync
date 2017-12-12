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
	[RoutePrefix("v1")]
	public class UploadController : ApiController
	{
		private static NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

		private static string S_UploadFolder = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Uploads");

		//[ValidateMimeMultipartContentFilter]
		[Route("upload/{clientLibrary}")]
		public async Task<IHttpActionResult> PostUpload(string clientLibrary)
		{
			var x = await Request.Content.ReadAsStreamAsync();

			string targetPath = Path.Combine(S_UploadFolder, clientLibrary + ".xml");

			IEnumerable<string> values;
			if(Request.Headers.TryGetValues("Content-Type", out values))
			{
				if(values.FirstOrDefault() == "application/x-gzip") 
					targetPath += ".gz";
			}

			using(FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				Log.Info("writing to '{0}'.", targetPath);
				await x.CopyToAsync(fs);
			}

			new Task(() =>
				{
					SendNotificationEmail(clientLibrary);
				}).Start();

			Log.Debug("Returning accepted.");
			return StatusCode(HttpStatusCode.Accepted);
		}

		//[ValidateMimeMultipartContentFilter]
		[Route("content")]
		public async Task<IHttpActionResult> PostContent()
		{

			var streamProvider = new MultipartFormDataStreamProvider(S_UploadFolder);
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
				Log.Warn("Dumping mail message to log.\n{0}", msg.Body.ToString());
				throw;
			}
		}

	}
}
