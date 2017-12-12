using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;

namespace CodeSyncWeb.Components
{
	public class GlobalExceptionMiddleware : OwinMiddleware
	{
		public GlobalExceptionMiddleware(OwinMiddleware next)
			: base(next)
		{ }

		public override async Task Invoke(IOwinContext context)
		{
			try
			{
				await Next.Invoke(context);
			}
			catch(Exception ex)
			{
				NLog.ILogger Log = NLog.LogManager.GetLogger("GlobalException");
				Log.Error(ex, "Unhandled exception");
			}
		}
	}
}