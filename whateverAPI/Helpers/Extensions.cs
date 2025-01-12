using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace whateverAPI.Helpers;


    /// <summary>
    /// Provides extension methods for creating and customizing ProblemDetails responses in ASP.NET Core applications.
    /// These extensions simplify the process of generating consistent, RFC 7807-compliant problem details
    /// while maintaining a fluent and expressive API.
    /// </summary>
    /// <remarks>
    /// The extensions in this class follow a fluent builder pattern, allowing for method chaining
    /// to construct problem details responses. Each method is designed to be composable,
    /// letting you build exactly the response you need while maintaining consistent structure.
    /// 
    /// Example usage:
    /// <code>
    /// return context.CreateProblemDetails()
    ///     .WithStatus(StatusCodes.Status404NotFound)
    ///     .WithTitle("Resource Not Found")
    ///     .WithDetail("The requested item could not be found")
    ///     .WithExtension("resourceId", id)
    ///     .ToProblemResult();
    /// </code>
    /// </remarks>
    public static class ProblemDetailsExtensions
    {
        /// <summary>
        /// Creates a new ProblemDetails instance with standard context information.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <returns>A new ProblemDetails instance initialized with context information.</returns>
        private static ProblemDetails CreateProblemDetails(this HttpContext context)
        {
            var problem = new ProblemDetails
            {
                Instance = context.Request.Path
            };

            return problem.WithContext(context);
        }

        /// <summary>
        /// Enriches a ProblemDetails instance with standard context information.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to enrich.</param>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <returns>The enriched ProblemDetails instance.</returns>
        private static ProblemDetails WithContext(this ProblemDetails problem, HttpContext context)
        {
            problem.Instance ??= context.Request.Path;

            // Add standard diagnostic information
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
            problem.Extensions["timestamp"] = DateTimeOffset.UtcNow;
            problem.Extensions["requestMethod"] = context.Request.Method;
            problem.Extensions["endpoint"] = context.Request.Path;

            return problem;
        }

        /// <summary>
        /// Sets the status code for a ProblemDetails instance and automatically updates the type URL.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to modify.</param>
        /// <param name="statusCode">The HTTP status code to set.</param>
        /// <returns>The modified ProblemDetails instance.</returns>
        private static ProblemDetails WithStatus(this ProblemDetails problem, int statusCode)
        {
            problem.Status = statusCode;
            problem.Type = $"https://httpstatuses.com/{statusCode}";
            return problem;
        }

        /// <summary>
        /// Sets the title for a ProblemDetails instance.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to modify.</param>
        /// <param name="title">The title describing the problem type.</param>
        /// <returns>The modified ProblemDetails instance.</returns>
        private static ProblemDetails WithTitle(this ProblemDetails problem, string title)
        {
            problem.Title = title;
            return problem;
        }

        /// <summary>
        /// Sets the detail message for a ProblemDetails instance.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to modify.</param>
        /// <param name="detail">A detailed explanation of the problem.</param>
        /// <returns>The modified ProblemDetails instance.</returns>
        private static ProblemDetails WithDetail(this ProblemDetails problem, string detail)
        {
            problem.Detail = detail;
            return problem;
        }

        /// <summary>
        /// Adds a single extension property to a ProblemDetails instance.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to modify.</param>
        /// <param name="key">The key for the extension property.</param>
        /// <param name="value">The value for the extension property.</param>
        /// <returns>The modified ProblemDetails instance.</returns>
        private static ProblemDetails WithExtension(this ProblemDetails problem, string key, object? value)
        {
            problem.Extensions[key] = value;
            return problem;
        }

        /// <summary>
        /// Adds multiple extension properties to a ProblemDetails instance.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to modify.</param>
        /// <param name="extensions">A dictionary of extension properties to add.</param>
        /// <returns>The modified ProblemDetails instance.</returns>
        private static ProblemDetails WithExtensions(this ProblemDetails problem, IDictionary<string, object?> extensions)
        {
            foreach (var extension in extensions)
            {
                problem.Extensions[extension.Key] = extension.Value;
            }

            return problem;
        }

        /// <summary>
        /// Converts a ProblemDetails instance to an IResult that can be returned from an endpoint.
        /// </summary>
        /// <param name="problem">The ProblemDetails instance to convert.</param>
        /// <returns>An IResult representing the problem details response.</returns>
        private static ProblemHttpResult ToProblemResult(this ProblemDetails problem) => TypedResults.Problem(problem);


        /// <summary>
        /// Creates a not found problem details response for a specific resource.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="resourceType">The type of resource that wasn't found.</param>
        /// <param name="identifier">The identifier that was used to look up the resource.</param>
        /// <returns>An IResult representing a not found response.</returns>
        public static IResult CreateNotFoundProblem(this HttpContext context, string resourceType, string identifier)
        {
            return context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status404NotFound)
                .WithTitle("Resource Not Found")
                .WithDetail($"{resourceType} with identifier '{identifier}' was not found")
                .WithExtensions(new Dictionary<string, object?>
                {
                    ["resourceType"] = resourceType,
                    ["identifier"] = identifier
                })
                .ToProblemResult();
        }

        /// <summary>
        /// Creates a validation problem details response.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="detail">A description of the validation problem.</param>
        /// <param name="validationErrors">Optional dictionary of field-specific validation errors.</param>
        /// <returns>An IResult representing a validation error response.</returns>
        public static IResult CreateValidationProblem(
            this HttpContext context,
            string detail,
            IDictionary<string, string[]>? validationErrors = null)
        {
            var problem = context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status422UnprocessableEntity)
                .WithTitle("Validation Failed")
                .WithDetail(detail);

            if (validationErrors != null)
            {
                problem.WithExtension("validationErrors", validationErrors);
            }

            return problem.ToProblemResult();
        }

        /// <summary>
        /// Creates a service unavailable problem details response.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="detail">A description of why the service is unavailable.</param>
        /// <param name="retryAfter">Optional TimeSpan indicating when to retry the request.</param>
        /// <returns>An IResult representing a service unavailable response.</returns>
        public static IResult CreateServiceUnavailableProblem(
            this HttpContext context,
            string detail,
            TimeSpan? retryAfter = null)
        {
            var problem = context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status503ServiceUnavailable)
                .WithTitle("Service Unavailable")
                .WithDetail(detail);

            if (retryAfter.HasValue)
            {
                problem.WithExtension("retryAfter", retryAfter.Value.TotalSeconds);
            }

            return problem.ToProblemResult();
        }

        /// <summary>
        /// Creates an unprocessable entity problem details response when an operation fails.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="operation">The name of the operation that failed.</param>
        /// <param name="detail">Optional detailed message. If not provided, constructs a standard message.</param>
        /// <returns>An IResult representing an unprocessable entity response.</returns>
        public static IResult CreateUnprocessableEntityProblem(
            this HttpContext context,
            string operation,
            string? detail = null)
        {
            return context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status422UnprocessableEntity)
                .WithTitle($"{operation} Failed")
                .WithDetail(detail ?? $"Failed to {operation.ToLower()} with the provided data")
                .ToProblemResult();
        }

        /// <summary>
        /// Creates an unauthorized problem details response.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="detail">A description of why authorization failed.</param>
        /// <returns>An IResult representing an unauthorized response.</returns>
        public static IResult CreateUnauthorizedProblem(
            this HttpContext context,
            string detail = "Authentication is required to access this resource")
        {
            return context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status401Unauthorized)
                .WithTitle("Unauthorized")
                .WithDetail(detail)
                .ToProblemResult();
        }

        /// <summary>
        /// Creates a forbidden problem details response.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="detail">A description of why access is forbidden.</param>
        /// <returns>An IResult representing a forbidden response.</returns>
        public static IResult CreateForbiddenProblem(
            this HttpContext context,
            string detail = "You do not have permission to access this resource")
        {
            return context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status403Forbidden)
                .WithTitle("Forbidden")
                .WithDetail(detail)
                .ToProblemResult();
        }

        /// <summary>
        /// Creates a bad request problem details response.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="detail">A description of why the request was bad.</param>
        /// <returns>An IResult representing a bad request response.</returns>
        public static IResult CreateBadRequestProblem(
            this HttpContext context,
            string detail)
        {
            return context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status400BadRequest)
                .WithTitle("Bad Request")
                .WithDetail(detail)
                .ToProblemResult();
        }

        /// <summary>
        /// Creates a problem details response for external service errors.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        /// <param name="serviceName">The name of the external service that failed.</param>
        /// <param name="detail">A description of the external service error.</param>
        /// <param name="exception">Optional exception that caused the error.</param>
        /// <returns>An IResult representing an external service error response.</returns>
        public static IResult CreateExternalServiceProblem(
            this HttpContext context,
            string serviceName,
            string detail,
            Exception? exception = null)
        {
            return context.CreateProblemDetails()
                .WithStatus(StatusCodes.Status502BadGateway)
                .WithTitle("External Service Error")
                .WithDetail(detail)
                .WithExtensions(new Dictionary<string, object?>
                {
                    ["service"] = serviceName,
                    ["errorType"] = exception?.GetType().Name,
                    ["errorDetails"] = exception?.Message
                })
                .ToProblemResult();
        }
    }
