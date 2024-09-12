using Microsoft.AspNetCore.Http;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.ViewModels.Application;
using RadialReview.Utilities;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers
{
	public class HandleReadonlyModeExceptions : IExceptionHandler
	{
		public bool CanProcess(ExceptionHandlerContext ex)
		{
			using (var s = HibernateSession.CreateOuterSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var res = AdminAccessor.InReadOnly(s);
					tx.Commit();
					s.Flush();
					return res;
				}
			}
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext)
		{
			try
			{
				var navBar = NavBarViewModel.CreateErrorNav();
				// Uses custom read only page.
				var view = ViewUtility.RenderView("~/Views/ReadOnly/Error.cshtml", handlerContext.Exception);
				if (!string.IsNullOrWhiteSpace(handlerContext.Exception.Message))
				{
					view.ViewData["Message"] = handlerContext.Exception.Message;
				}
				view.ViewData["HasBaseController"] = true;
				view.ViewData["NavBar"] = navBar;
				handlerContext.HttpContext.Response.Clear();
				handlerContext.HttpContext.Response.StatusCode = 500;
				await handlerContext.HttpContext.Response.WriteAsync(await view.ExecuteAsync());
			}
			catch (Exception e)
			{
				await handlerContext.RenderRawHtml("The service is in read-only mode. An unhandled error occurred.");
			}
		}
	}
}
