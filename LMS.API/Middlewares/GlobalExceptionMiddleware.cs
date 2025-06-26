using System.Net;
using System.Text.Json;
using LMS_Backend.LMS.Common.Exceptions;

namespace LMS_Backend.LMS.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized);
            }
            catch (BadHttpRequestException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
            }
            catch (KeyNotFoundException ex)
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
            }
            catch (Exception ex) when (IsGenericException(ex, typeof(AlreadyExistsException<>)))
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.Conflict);
            }
            catch (Exception ex) when (IsGenericException(ex, typeof(DataNotFoundException<>)))
            {
                await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
            }
        }

        private static bool IsGenericException(Exception ex, Type genericType)
        {
            return ex.GetType().IsGenericType &&
                   ex.GetType().GetGenericTypeDefinition() == genericType;
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = GetUserFriendlyMessage(exception, statusCode),
                Detailed = _env.IsDevelopment() ? exception.Message : null,
                StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
                EntityType = GetEntityTypeFromException(exception)
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static string GetUserFriendlyMessage(Exception exception, HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.Unauthorized)
                return "Unauthorized access";
            if (statusCode == HttpStatusCode.BadRequest)
                return "Invalid request";
            if (statusCode == HttpStatusCode.NotFound && IsGenericException(exception, typeof(DataNotFoundException<>)))
                return exception.Message ?? "The requested data was not found";
            if (statusCode == HttpStatusCode.Conflict && IsGenericException(exception, typeof(AlreadyExistsException<>)))
                return exception.Message ?? "The resource already exists";
            if (statusCode == HttpStatusCode.NotFound)
                return "Resource not found";
            if (statusCode == HttpStatusCode.InternalServerError)
                return "An internal server error occurred";

            return exception.Message;
        }

        private static string? GetEntityTypeFromException(Exception exception)
        {
            if (IsGenericException(exception, typeof(DataNotFoundException<>)))
                return exception.GetType().GenericTypeArguments[0].Name;

            if (IsGenericException(exception, typeof(AlreadyExistsException<>)))
                return exception.GetType().GenericTypeArguments[0].Name;

            return null;
        }
    }

    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}