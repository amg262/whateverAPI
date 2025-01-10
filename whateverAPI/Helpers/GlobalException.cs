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
/// Global exception handler that provides detailed, structured error responses
/// while maintaining security and following best practices for error handling.
/// </summary>
internal sealed class GlobalException : IExceptionHandler
{
    private readonly ILogger<GlobalException> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GlobalException(ILogger<GlobalException> logger, IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, statusDescription) = GetExceptionDetails(exception);

        // Log with correlation ID for better tracing
        var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId,
                   ["Path"] = httpContext.Request.Path,
                   ["Method"] = httpContext.Request.Method
               }))
        {
            _logger.LogError(
                exception,
                "Request failed with status {StatusCode}: {Description}",
                statusCode,
                statusDescription);
        }

        var problemDetails = CreateProblemDetails(
            httpContext,
            statusCode,
            statusDescription,
            exception,
            correlationId);

        if (problemDetails.Status != null) httpContext.Response.StatusCode = problemDetails.Status.Value;

        // Set appropriate headers
        httpContext.Response.Headers.Append("X-Correlation-ID", correlationId);
        httpContext.Response.Headers.Append("X-Error-Type", exception.GetType().Name);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private (int StatusCode, string Description) GetExceptionDetails(Exception exception) => exception switch
    {
        // HTTP and Network Errors
        HttpRequestException httpEx => ((int)(httpEx.StatusCode ?? HttpStatusCode.InternalServerError),
            $"HTTP Request Error: {GetSafeMessage(httpEx)}"),

        // Data and Validation Errors
        JsonException jsonEx => (StatusCodes.Status400BadRequest,
            $"Invalid JSON format: {GetSafeMessage(jsonEx)}"),
        ArgumentException argEx => (StatusCodes.Status400BadRequest,
            $"Invalid parameter '{argEx.ParamName}': {GetSafeMessage(argEx)}"),
        ValidationException valEx => (StatusCodes.Status422UnprocessableEntity,
            FormatValidationError(valEx)),

        // Database Errors
        SqlException sqlEx => (StatusCodes.Status500InternalServerError,
            GetDatabaseErrorMessage(sqlEx)),
        DbException dbEx => (StatusCodes.Status500InternalServerError,
            GetDatabaseErrorMessage(dbEx)),

        // Security Errors
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
            "Authentication required"),
        SecurityException => (StatusCodes.Status403Forbidden,
            "Insufficient permissions"),

        // Resource Errors
        KeyNotFoundException => (StatusCodes.Status404NotFound,
            "The requested resource was not found"),
        FileNotFoundException fileEx => (StatusCodes.Status404NotFound,
            $"File not found: {Path.GetFileName(fileEx.FileName)}"),

        // Operational Errors
        TimeoutException => (StatusCodes.Status408RequestTimeout,
            "The request timed out"),
        OperationCanceledException => (StatusCodes.Status499ClientClosedRequest,
            "The operation was canceled"),

        // Infrastructure Errors
        SocketException sockEx => (StatusCodes.Status503ServiceUnavailable,
            $"Network error {sockEx.ErrorCode}: {GetSafeMessage(sockEx)}"),

        // Default case
        _ => (StatusCodes.Status500InternalServerError,
            "An unexpected error occurred")
    };

    private ProblemDetails CreateProblemDetails(
        HttpContext context,
        int statusCode,
        string description,
        Exception exception,
        string correlationId)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetStatusDescription(statusCode),
            Detail = description,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}",
        };

        // Add extended properties based on environment
        var extensions = new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["requestMethod"] = context.Request.Method,
        };

        // Include technical details only in development
        if (_environment.IsDevelopment())
        {
            extensions.Add("exceptionType", exception.GetType().FullName);
            extensions.Add("stackTrace", exception.StackTrace);

            if (exception.InnerException != null)
            {
                extensions.Add("innerException", new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message,
                    stackTrace = exception.InnerException.StackTrace
                });
            }
        }

        problemDetails.Extensions = extensions;
        return problemDetails;
    }

    private static string FormatValidationError(ValidationException exception)
    {
        // Convert FluentValidation errors into a more readable format
        var errorDetails = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray()
            );

        return JsonSerializer.Serialize(new
        {
            message = "Validation failed",
            errors = errorDetails
        });
    }

    private string GetDatabaseErrorMessage(Exception dbException)
    {
        // In production, don't expose internal database errors
        if (!_environment.IsDevelopment())
        {
            return "A database error occurred";
        }

        return dbException switch
        {
            SqlException sqlEx => $"Database error {sqlEx.Number}: {GetSafeMessage(sqlEx)}",
            DbException dbEx => $"Database error: {GetSafeMessage(dbEx)}",
            _ => "An unexpected database error occurred"
        };
    }

    private string GetSafeMessage(Exception exception)
    {
        // In production, we might want to sanitize or limit certain error messages
        if (!_environment.IsDevelopment() && exception is SqlException or DbException)
        {
            return "An error occurred while processing your request";
        }

        return exception.Message;
    }

    private static string GetStatusDescription(int statusCode) => statusCode switch
    {
        StatusCodes.Status100Continue => "Continue",
        StatusCodes.Status101SwitchingProtocols => "Switching Protocols",
        StatusCodes.Status102Processing => "Processing",
        StatusCodes.Status200OK => "OK",
        StatusCodes.Status201Created => "Created",
        StatusCodes.Status202Accepted => "Accepted",
        StatusCodes.Status204NoContent => "No Content",
        StatusCodes.Status205ResetContent => "Reset Content",
        StatusCodes.Status206PartialContent => "Partial Content",
        StatusCodes.Status207MultiStatus => "Multi-Status",
        StatusCodes.Status208AlreadyReported => "Already Reported",
        StatusCodes.Status226IMUsed => "IM Used",
        StatusCodes.Status300MultipleChoices => "Multiple Choices",
        StatusCodes.Status301MovedPermanently => "Moved Permanently",
        StatusCodes.Status302Found => "Found",
        StatusCodes.Status303SeeOther => "See Other",
        StatusCodes.Status304NotModified => "Not Modified",
        StatusCodes.Status305UseProxy => "Use Proxy",
        StatusCodes.Status306SwitchProxy => "Switch Proxy",
        StatusCodes.Status307TemporaryRedirect => "Temporary Redirect",
        StatusCodes.Status308PermanentRedirect => "Permanent Redirect",
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status402PaymentRequired => "Payment Required",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status405MethodNotAllowed => "Method Not Allowed",
        StatusCodes.Status406NotAcceptable => "Not Acceptable",
        StatusCodes.Status407ProxyAuthenticationRequired => "Proxy Authentication Required",
        StatusCodes.Status408RequestTimeout => "Request Timeout",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status410Gone => "Gone",
        StatusCodes.Status411LengthRequired => "Length Required",
        StatusCodes.Status412PreconditionFailed => "Precondition Failed",
        StatusCodes.Status413PayloadTooLarge => "Payload Too Large",
        StatusCodes.Status415UnsupportedMediaType => "Unsupported Media Type",
        StatusCodes.Status416RangeNotSatisfiable => "Range Not Satisfiable",
        StatusCodes.Status417ExpectationFailed => "Expectation Failed",
        StatusCodes.Status418ImATeapot => "I'm a teapot",
        StatusCodes.Status421MisdirectedRequest => "Misdirected Request",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        StatusCodes.Status423Locked => "Locked",
        StatusCodes.Status424FailedDependency => "Failed Dependency",
        StatusCodes.Status426UpgradeRequired => "Upgrade Required",
        StatusCodes.Status428PreconditionRequired => "Precondition Required",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        StatusCodes.Status431RequestHeaderFieldsTooLarge => "Request Header Fields Too Large",
        StatusCodes.Status451UnavailableForLegalReasons => "Unavailable For Legal Reasons",
        StatusCodes.Status499ClientClosedRequest => "Client Closed Request",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        StatusCodes.Status501NotImplemented => "Not Implemented",
        StatusCodes.Status502BadGateway => "Bad Gateway",
        StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
        StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
        StatusCodes.Status506VariantAlsoNegotiates => "Variant Also Negotiates",
        StatusCodes.Status507InsufficientStorage => "Insufficient Storage",
        StatusCodes.Status508LoopDetected => "Loop Detected",
        StatusCodes.Status510NotExtended => "Not Extended",
        StatusCodes.Status511NetworkAuthenticationRequired => "Network Authentication Required",
        _ => "Unknown Status Code"
    };
}
// using System.Diagnostics;
// using System.Net;
// using System.Text.Json;
// using Microsoft.AspNetCore.Diagnostics;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Data.SqlClient;
//
// namespace whateverAPI.Helpers;
//
// /// <summary>
// /// Global exception handler for handling exceptions across the entire application.
// /// Implements <see cref="IExceptionHandler"/> to provide a unified way of handling exceptions.
// /// </summary>
// internal sealed class GlobalException(ILogger<GlobalException> logger) : IExceptionHandler
// {
//     /// <summary>
//     /// Asynchronously handles exceptions and generates a structured error response.
//     /// </summary>
//     /// <param name="httpContext">The context of the current HTTP request.</param>
//     /// <param name="exception">The exception that occurred.</param>
//     /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
//     /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the exception was handled.</returns>
//     public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
//     {
//         logger.LogError(exception, "Exception occurred: {Message}", exception.Message);
//
//         int statusCode;
//         string statusDescription;
//
//         switch (exception)
//         {
//             case HttpRequestException httpRequestException:
//                 statusCode = (int)(httpRequestException.StatusCode ?? HttpStatusCode.InternalServerError);
//                 statusDescription = $"HTTP Request Error: {httpRequestException.Message}";
//                 break;
//             case JsonException jsonException:
//                 statusCode = StatusCodes.Status400BadRequest;
//                 statusDescription = $"Invalid JSON: {jsonException.Message}";
//                 break;
//             case ArgumentException argumentException:
//                 statusCode = StatusCodes.Status400BadRequest;
//                 statusDescription = $"Invalid Argument Param: {argumentException.ParamName} Message: {argumentException.Message}";
//                 break;
//             case InvalidOperationException invalidOpException:
//                 statusCode = StatusCodes.Status400BadRequest;
//                 statusDescription = $"Invalid Operation: {invalidOpException.Message}";
//                 break;
//             case UnauthorizedAccessException unauthorizedException:
//                 statusCode = StatusCodes.Status401Unauthorized;
//                 statusDescription = $"Unauthorized Access: {unauthorizedException.Message}";
//                 break;
//             case KeyNotFoundException keyNotFoundException:
//                 statusCode = StatusCodes.Status404NotFound;
//                 statusDescription = $"Resource Not Found: {keyNotFoundException.Message}";
//                 break;
//             case TimeoutException timeoutException:
//                 statusCode = StatusCodes.Status408RequestTimeout;
//                 statusDescription = $"Request Timeout: {timeoutException.Message}";
//                 break;
//             case NotImplementedException notImplementedException:
//                 statusCode = StatusCodes.Status501NotImplemented;
//                 statusDescription = $"Not Implemented: {notImplementedException.Message}";
//                 break;
//             case OperationCanceledException operationCanceledException:
//                 statusCode = StatusCodes.Status499ClientClosedRequest;
//                 statusDescription = $"Operation Canceled: {operationCanceledException.Message}";
//                 break;
//             case SqlException sqlException:
//                 statusCode = StatusCodes.Status500InternalServerError;
//                 statusDescription = $"SQL Error {sqlException.Number}: {sqlException.Message}";
//                 logger.LogError(sqlException, "SQL Exception: {Message}, Number: {Number}", sqlException.Message,
//                     sqlException.Number);
//                 break;
//             case System.Data.Common.DbException dbException:
//                 statusCode = StatusCodes.Status500InternalServerError;
//                 statusDescription = $"Database Error: {dbException.Message}";
//                 logger.LogError(dbException, "Database Exception: {Message}", dbException.Message);
//                 break;
//             case FormatException formatException:
//                 statusCode = StatusCodes.Status400BadRequest;
//                 statusDescription = $"Invalid Format: {formatException.Message}";
//                 break;
//             case OverflowException overflowException:
//                 statusCode = StatusCodes.Status400BadRequest;
//                 statusDescription = $"Arithmetic Overflow: {overflowException.Message}";
//                 break;
//             case IOException ioException:
//                 statusCode = StatusCodes.Status500InternalServerError;
//                 statusDescription = $"I/O Error: {ioException.Message}";
//                 logger.LogError(ioException, "I/O Exception: {Message}", ioException.Message);
//                 break;
//             case System.Net.Sockets.SocketException socketException:
//                 statusCode = StatusCodes.Status503ServiceUnavailable;
//                 statusDescription = $"Network Error {socketException.ErrorCode}: {socketException.Message}";
//                 logger.LogError(socketException, "Socket Exception: {Message}, ErrorCode: {ErrorCode}", socketException.Message,
//                     socketException.ErrorCode);
//                 break;
//             case System.Security.SecurityException securityException:
//                 statusCode = StatusCodes.Status403Forbidden;
//                 statusDescription = $"Security Violation: {securityException.Message}";
//                 break;
//             default:
//                 statusCode = httpContext.Response.StatusCode;
//                 statusDescription = GetStatusDescription(statusCode);
//                 break;
//         }
//
//         var problemDetails = new ProblemDetails
//         {
//             Status = statusCode,
//             Title = statusDescription,
//             Detail = exception?.InnerException?.Message ?? exception?.Message,
//             Instance = httpContext.Request.Path,
//             Type = exception?.GetType().Name,
//             Extensions = new Dictionary<string, object>
//             {
//                 { "traceId", Activity.Current?.Id ?? httpContext.TraceIdentifier },
//                 { "stackTrace", exception?.StackTrace }
//             }
//         };
//
//         httpContext.Response.StatusCode = problemDetails.Status.Value;
//
//         await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
//
//         return true;
//     }
//
//     private static string GetStatusDescription(int statusCode) => statusCode switch
//     {
//         StatusCodes.Status100Continue => "Continue",
//         StatusCodes.Status101SwitchingProtocols => "Switching Protocols",
//         StatusCodes.Status102Processing => "Processing",
//         StatusCodes.Status200OK => "OK",
//         StatusCodes.Status201Created => "Created",
//         StatusCodes.Status202Accepted => "Accepted",
//         StatusCodes.Status204NoContent => "No Content",
//         StatusCodes.Status205ResetContent => "Reset Content",
//         StatusCodes.Status206PartialContent => "Partial Content",
//         StatusCodes.Status207MultiStatus => "Multi-Status",
//         StatusCodes.Status208AlreadyReported => "Already Reported",
//         StatusCodes.Status226IMUsed => "IM Used",
//         StatusCodes.Status300MultipleChoices => "Multiple Choices",
//         StatusCodes.Status301MovedPermanently => "Moved Permanently",
//         StatusCodes.Status302Found => "Found",
//         StatusCodes.Status303SeeOther => "See Other",
//         StatusCodes.Status304NotModified => "Not Modified",
//         StatusCodes.Status305UseProxy => "Use Proxy",
//         StatusCodes.Status306SwitchProxy => "Switch Proxy",
//         StatusCodes.Status307TemporaryRedirect => "Temporary Redirect",
//         StatusCodes.Status308PermanentRedirect => "Permanent Redirect",
//         StatusCodes.Status400BadRequest => "Bad Request",
//         StatusCodes.Status401Unauthorized => "Unauthorized",
//         StatusCodes.Status402PaymentRequired => "Payment Required",
//         StatusCodes.Status403Forbidden => "Forbidden",
//         StatusCodes.Status404NotFound => "Not Found",
//         StatusCodes.Status405MethodNotAllowed => "Method Not Allowed",
//         StatusCodes.Status406NotAcceptable => "Not Acceptable",
//         StatusCodes.Status407ProxyAuthenticationRequired => "Proxy Authentication Required",
//         StatusCodes.Status408RequestTimeout => "Request Timeout",
//         StatusCodes.Status409Conflict => "Conflict",
//         StatusCodes.Status410Gone => "Gone",
//         StatusCodes.Status411LengthRequired => "Length Required",
//         StatusCodes.Status412PreconditionFailed => "Precondition Failed",
//         StatusCodes.Status413PayloadTooLarge => "Payload Too Large",
//         StatusCodes.Status415UnsupportedMediaType => "Unsupported Media Type",
//         StatusCodes.Status416RangeNotSatisfiable => "Range Not Satisfiable",
//         StatusCodes.Status417ExpectationFailed => "Expectation Failed",
//         StatusCodes.Status418ImATeapot => "I'm a teapot",
//         StatusCodes.Status421MisdirectedRequest => "Misdirected Request",
//         StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
//         StatusCodes.Status423Locked => "Locked",
//         StatusCodes.Status424FailedDependency => "Failed Dependency",
//         StatusCodes.Status426UpgradeRequired => "Upgrade Required",
//         StatusCodes.Status428PreconditionRequired => "Precondition Required",
//         StatusCodes.Status429TooManyRequests => "Too Many Requests",
//         StatusCodes.Status431RequestHeaderFieldsTooLarge => "Request Header Fields Too Large",
//         StatusCodes.Status451UnavailableForLegalReasons => "Unavailable For Legal Reasons",
//         StatusCodes.Status499ClientClosedRequest => "Client Closed Request",
//         StatusCodes.Status500InternalServerError => "Internal Server Error",
//         StatusCodes.Status501NotImplemented => "Not Implemented",
//         StatusCodes.Status502BadGateway => "Bad Gateway",
//         StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
//         StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
//         StatusCodes.Status506VariantAlsoNegotiates => "Variant Also Negotiates",
//         StatusCodes.Status507InsufficientStorage => "Insufficient Storage",
//         StatusCodes.Status508LoopDetected => "Loop Detected",
//         StatusCodes.Status510NotExtended => "Not Extended",
//         StatusCodes.Status511NetworkAuthenticationRequired => "Network Authentication Required",
//         _ => "Unknown Status Code"
//     };
// }