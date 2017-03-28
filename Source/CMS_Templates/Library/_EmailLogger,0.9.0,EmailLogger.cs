using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrownPeak.CMSAPI;
using CrownPeak.CMSAPI.Services;
/* Some Namespaces are not allowed. */
namespace CrownPeak.CMSAPI.CustomLibrary
{
	/// <summary>
	/// Logger that sends output to specified recipients
	/// </summary>
	/// <remarks>
	/// Not thread safe.</remarks>
	public class EmailLogger : IDisposable
	{
		private EmailLogger()
		{
			this.IsDebugEnabled = false;
			this.IsInfoEnabled = true;
			this.IsErrorEnabled = true;
			this.IsWarnEnabled = true;
		}
		public EmailLogger(string subject, string recipients)
			: this()
		{
			this.Subject = subject;
			this.Recipients = recipients;
		}

		StringBuilder sb = new StringBuilder();

		public bool IsDebugEnabled { get; set; }
		public bool IsInfoEnabled { get; set; }
		public bool IsWarnEnabled { get; set; }
		public bool IsErrorEnabled { get; set; }

		public void Info(string message)
		{
			if(IsInfoEnabled)
			{
				sb.Append("INFO ").AppendLine(message).AppendLine();
			}
		}
		public void Info(string format, params object[] args)
		{
			if(IsInfoEnabled)
			{
				sb.Append("INFO ").AppendFormat(format, args).AppendLine();
			}
		}
		public void Debug(string format, params object[] args)
		{
			if(IsDebugEnabled)
			{
				sb.Append("DEBUG ").AppendFormat(format, args).AppendLine();
			}
		}
		public void Error(Exception exception, string format, params object[] args)
		{
			if(IsErrorEnabled)
			{
				sb.Append("ERROR ").AppendFormat(format, args).Append(" ").Append(exception.ToString()).AppendLine();
			}
		}
		public void Error(Exception exception, string message)
		{
			if(IsErrorEnabled)
			{
				sb.Append("ERROR ").Append(" ").Append(exception.ToString()).AppendLine();
			}
		}

		public string Subject { get; set; }
		public string Recipients { get; set; }

		public string GetLog() { return sb.ToString(); }
		public void Flush()
		{
			if(sb.Length > 0)
			{
				var recipients = this.Recipients.Split(';');

				if(recipients.Count() > 0)
				{
					Util.Email(Subject, sb.ToString(), recipients.ToList(), contentType: CrownPeak.CMSAPI.ContentType.TextPlain);
				}
			}

			sb.Clear(); //yes i know, multithreaded access will hurt this.
		}

		#region IDisposable
		private bool _disposed;
		public void Dispose()
		{
			if(_disposed == false)
			{
				Dispose(true);
			}
			_disposed = true;
		}
		#endregion
		private void Dispose(bool disposing)
		{
			this.Flush();
		}
	}
}
