using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ResourcePlanPro.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var result = string.Empty;

            switch (exception)
            {
                case KeyNotFoundException:
                    code = HttpStatusCode.NotFound;
                    result = JsonSerializer.Serialize(new { 
                        success = false,
                        message = exception.Message 
                    });
                    break;
                    
                case UnauthorizedAccessException:
                    code = HttpStatusCode.Unauthorized;
                    result = JsonSerializer.Serialize(new { 
                        success = false,
                        message = "Unauthorized access" 
                    });
                    break;
                    
                case ArgumentException:
                case InvalidOperationException:
                    code = HttpStatusCode.BadRequest;
                    result = JsonSerializer.Serialize(new { 
                        success = false,
                        message = exception.Message 
                    });
                    break;
                    
                default:
                    result = JsonSerializer.Serialize(new { 
                        success = false,
                        message = "An internal server error occurred. Please try again later." 
                    });
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result);
        }
    }

    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
