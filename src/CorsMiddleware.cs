using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

namespace Scarlet
{
	// https://stackoverflow.com/a/45844400
	public class CorsMiddleware
	{
		private readonly RequestDelegate _requestDelegate;

		public CorsMiddleware(RequestDelegate requestDelegate)
		{
			_requestDelegate = requestDelegate;
		}

		public Task Invoke(HttpContext httpContext)
		{
			httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
			httpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
			httpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Date, X-Api-Version, X-File-Name");
			httpContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST,GET,PUT,PATCH,DELETE,OPTIONS");
			return _requestDelegate(httpContext);
		}
	}

	public static class CorsMiddlewareExtension
	{
		public static IApplicationBuilder UseCorsMiddleware(this IApplicationBuilder builder)
			=> builder.UseMiddleware<CorsMiddleware>();
	}
}