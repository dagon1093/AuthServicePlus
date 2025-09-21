using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace AuthServicePlus.Api.Middleware

{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteProblem(httpContext, StatusCodes.Status401Unauthorized, "Unauthorized", ex);
            }
            catch (System.Security.SecurityException ex)
            {
                await WriteProblem(httpContext, StatusCodes.Status403Forbidden, "Forbidden", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteProblem(httpContext, StatusCodes.Status500InternalServerError, "Unexpected error", ex);
            }


        }

        public static async Task WriteProblem(HttpContext httpContext, int status, string title, Exception ex)
        {
            httpContext.Response.ContentType = "application/problem+json";
            httpContext.Response.StatusCode = status;

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = ex.Message,
                Instance = httpContext.Request.Path,
                Type = "about:blank"
            };

            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
