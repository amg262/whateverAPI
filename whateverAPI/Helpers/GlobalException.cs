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
internal sealed class GlobalException : IExceptionHandler
{
    private readonly ILogger<GlobalException> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the GlobalException handler with required dependencies
    /// for error processing and environment-aware behavior.
    /// </summary>
    /// <param name="logger">Logger for error tracking and auditing</param>
    /// <param name="environment">Environment information for conditional behavior</param>
    /// <param name="httpContextAccessor">Access to HTTP context for request details</param>
    public GlobalException(ILogger<GlobalException> logger, IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Processes exceptions into structured HTTP responses while maintaining security and providing
    /// appropriate error details based on the environment.
    /// </summary>
    /// <param name="httpContext">The current HTTP context</param>
    /// <param name="exception">The exception to handle</param>
    /// <param name="cancellationToken">Token for cancellation support</param>
    /// <returns>True if the exception was handled; otherwise, false</returns>
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

    /// <summary>
    /// Processes exceptions to determine appropriate HTTP status codes and descriptions
    /// while maintaining security through message sanitization.
    /// </summary>
    /// <param name="exception">The exception to analyze</param>
    /// <returns>A tuple containing the HTTP status code and safe error description</returns>
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

    /// <summary>
    /// Creates a detailed problem response following RFC 7807 specifications while
    /// implementing environment-aware detail exposure.
    /// </summary>
    /// <param name="context">The HTTP context for request details</param>
    /// <param name="statusCode">The HTTP status code</param>
    /// <param name="description">The error description</param>
    /// <param name="exception">The original exception</param>
    /// <param name="correlationId">The correlation ID for tracking</param>
    /// <returns>A ProblemDetails object containing the error information</returns>
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

    /// <summary>
    /// Formats validation errors into a structured, client-friendly format
    /// while maintaining security through proper error aggregation.
    /// </summary>
    /// <remarks>
    /// This method implements a secure approach to validation error reporting:
    /// - Groups errors by property for clarity
    /// - Maintains error message integrity
    /// - Provides structured JSON output
    /// - Ensures consistent error format
    /// </remarks>
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

    /// <summary>
    /// Creates secure database error messages that avoid exposing sensitive
    /// information while providing useful feedback.
    /// </summary>
    /// <remarks>
    /// Implements environment-aware database error handling:
    /// - Development: Includes error codes and details
    /// - Production: Generic error messages only
    /// - Proper sanitization of error information
    /// - Consistent error format across environments
    /// </remarks>
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

    /// <summary>
    /// Creates secure, environment-aware error messages by sanitizing sensitive information
    /// from exception details.
    /// </summary>
    /// <param name="exception">The exception containing the original error message</param>
    /// <returns>A sanitized error message safe for external consumption</returns>
    /// <remarks>
    /// Usage Example:
    /// In development: "Violation of unique constraint 'IX_Users_Email'"
    /// In production: "An error occurred while processing your request"
    /// </remarks>
    private string GetSafeMessage(Exception exception)
    {
        // In production, we might want to sanitize or limit certain error messages
        if (!_environment.IsDevelopment() && exception is SqlException or DbException)
        {
            return "An error occurred while processing your request";
        }

        return exception.Message;
    }

    /// <summary>
    /// Provides standardized, human-readable descriptions for HTTP status codes
    /// following RFC 7231 and common web standards.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to describe</param>
    /// <returns>A standardized description of the status code</returns>
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