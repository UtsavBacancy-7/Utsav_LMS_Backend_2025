using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.LMS.API.Middlewares
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        private const string LogDirectory = "Logs";
        private const string LogFilePrefix = "api_";
        private const string LogFileExtension = ".log";

        public LogMiddleware(RequestDelegate next)
        {
            _next = next;

            // Ensure log directory exists
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var requestTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            string? errorMessage = null;

            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var responseTime = DateTime.UtcNow;
                var statusCode = httpContext.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Create log entry
                var logEntry = $"[{requestTime:yyyy-MM-dd HH:mm:ss.fff}] " +
                              $"Method: {httpContext.Request.Method} " +
                              $"Path: {httpContext.Request.Path} " +
                              $"Status: {statusCode} " +
                              $"Duration: {elapsedMs}ms " +
                              $"{(errorMessage != null ? $"Error: {errorMessage} " : "")}" +
                              $"{Environment.NewLine}";

                // Write to console
                WriteColoredConsoleLog(requestTime, responseTime, statusCode, httpContext, errorMessage);

                // Write to log file
                await WriteToLogFile(logEntry);
            }
        }

        private void WriteColoredConsoleLog(DateTime requestTime, DateTime responseTime, int statusCode, HttpContext httpContext, string? errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{requestTime:yyyy-MM-dd HH:mm:ss.fff}] {httpContext.Request.Method} {httpContext.Request.Path}");

            Console.ForegroundColor = statusCode switch
            {
                >= 200 and < 300 => ConsoleColor.Green,    // Success
                >= 300 and < 400 => ConsoleColor.Yellow,   // Redirection
                >= 400 and < 500 => ConsoleColor.Red,      // Client Error
                >= 500 and < 600 => ConsoleColor.DarkRed,  // Server Error
                _ => ConsoleColor.Gray
            };

            Console.WriteLine($"[{responseTime:yyyy-MM-dd HH:mm:ss.fff}] Status: {statusCode} {(errorMessage != null ? $"Error: {errorMessage}" : "")}");
            Console.ResetColor();
        }

        private async Task WriteToLogFile(string logEntry)
        {
            var logFileName = $"{LogFilePrefix}{DateTime.UtcNow:yyyyMMdd}{LogFileExtension}";
            var logFilePath = Path.Combine(LogDirectory, logFileName);

            try
            {
                await File.AppendAllTextAsync(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseLogMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogMiddleware>();
        }
    }
}