using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace CodeSyncWeb.Components
{
	public class OopsExceptionHandler : ExceptionHandler
	{
		public override void HandleCore(ExceptionHandlerContext context)
		{
			context.Result = new TextPlainErrorResult
			{
				Request = context.ExceptionContext.Request,
				Content = "Oops! Sorry! Something went wrong." +
						  "Please contact support@contoso.com so we can try to fix it."
			};
		}

		private class TextPlainErrorResult : IHttpActionResult
		{
			public HttpRequestMessage Request { get; set; }

			public string Content { get; set; }

			public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
			{
				HttpResponseMessage response =
								 new HttpResponseMessage(HttpStatusCode.InternalServerError);
				response.Content = new StringContent(Content);
				response.RequestMessage = Request;
				return Task.FromResult(response);
			}
		}
	}

	public class ExceptionHandler : IExceptionHandler
	{
		public virtual Task HandleAsync(ExceptionHandlerContext context,
										CancellationToken cancellationToken)
		{
			if(!ShouldHandle(context))
			{
				return Task.FromResult(0);
			}

			return HandleAsyncCore(context, cancellationToken);
		}

		public virtual Task HandleAsyncCore(ExceptionHandlerContext context,
										   CancellationToken cancellationToken)
		{
			HandleCore(context);
			return Task.FromResult(0);
		}

		public virtual void HandleCore(ExceptionHandlerContext context)
		{
		}

		public virtual bool ShouldHandle(ExceptionHandlerContext context)
		{
			return context.ExceptionContext.CatchBlock.IsTopLevel;
		}
	} 
}