using AuthServicePlus.Api.Middleware;

namespace AuthServicePlus.Api.Extensions
{
    public static class ExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) 
            => app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
