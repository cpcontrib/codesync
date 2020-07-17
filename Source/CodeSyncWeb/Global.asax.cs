using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

using System.Net.Http.Formatting;
using System.Net;

namespace CodeSyncWeb
{
	public class WebApiApplication : System.Web.HttpApplication
	{

		NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

		protected void Application_Start()
		{
			Log.Debug("Application Starting.");
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);

			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		}

		protected void Application_BeginRequest()
		{
			if(Log.IsDebugEnabled) Log.Debug("Begin request: {0} {1}", Context.Request.HttpMethod, Context.Request.RawUrl);
		}

		protected void Application_Error()
		{
			Log.Error(Server.GetLastError());
		}

	}
}

