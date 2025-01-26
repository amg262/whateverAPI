using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace whateverAPI.Helpers;

/// <summary>
/// Provides centralized exception handling for the entire application, implementing secure error reporting
/// while following RFC 7807 Problem Details for HTTP APIs specification.
/// </summary>
/// <remarks>
/// This exception handler serves as the application's central point for converting various types of exceptions
/// into structured, secure HTTP responses. It implements several important security and usability features:
/// </remarks>
public class GlobalException(ILogger<GlobalException> logger, ProblemDetailsConfig problemDetailsConfig) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Gather additional context for both logging and problem details
        var exceptionContext = new Dictionary<string, object?>
        {
            ["exceptionType"] = exception.GetType().FullName,
            ["exceptionMessage"] = exception.Message,
            ["stackTrace"] = exception.StackTrace
        };

        // Add inner exception details if present
        if (exception.InnerException != null)
        {
            exceptionContext["innerException"] = new
            {
                type = exception.InnerException.GetType().FullName,
                message = exception.InnerException.Message,
                stackTrace = exception.InnerException.StackTrace
            };
        }

        // Log the complete exception details with all available context
        logger.LogError(
            exception,
            "Unhandled exception occurred while processing request {Method} {Path}. Exception Details: {@ExceptionContext}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            exceptionContext);

        // Create problem details using the same context
        var problemDetails = problemDetailsConfig.CreateProblemDetails(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred while processing your request",
            exceptionContext);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}