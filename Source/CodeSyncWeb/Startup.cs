using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(CodeSyncWeb.Startup))]

namespace CodeSyncWeb
{
	public partial class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.Use<CodeSyncWeb.Components.GlobalExceptionMiddleware>();
			ConfigureAuth(app);
		}
	}
}
