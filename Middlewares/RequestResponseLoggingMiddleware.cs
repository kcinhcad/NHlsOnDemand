using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NHlsOnDemand.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Игнорируем Swagger (не логируем)
            if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.InvariantCultureIgnoreCase))
            {
                await _next(context);
                return;
            }

            var watch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception");
                throw;
            }
            finally
            {
                watch.Stop();

                _logger.LogInformation(
                    $"{context.Connection.RemoteIpAddress} {context.Request.Method} {context.Request.Path} " +
                    $"{context.Response.StatusCode} {watch.ElapsedMilliseconds}");
            }
        }
    }
}
