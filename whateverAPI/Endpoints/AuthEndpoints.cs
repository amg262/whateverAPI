using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Options;
using whateverAPI.Services;

namespace whateverAPI.Endpoints;

public class AuthEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var microsoftAuthGroup = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/auth/microsoft")
            .WithTags("Authentication")
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(Helper.AuthPolicy);

        var googleAuthGroup = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/auth/google")
            .WithTags("Authentication")
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(Helper.AuthPolicy);

        var facebookAuthGroup = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/auth/facebook")
            .WithTags("Authentication")
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(Helper.AuthPolicy);

        var authGroup = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/auth")
            .WithTags("Authentication")
            .HasApiVersion(new ApiVersion(1, 0));


        authGroup.MapGet("/status", async Task<IResult> (
                HttpContext context,
                IJwtTokenService jwtTokenService) =>
            {
                var token = jwtTokenService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return TypedResults.Ok(new { isAuthenticated = false });
                }

                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var name = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                return TypedResults.Ok(new
                {
                    isAuthenticated = true,
                    userId,
                    email,
                    name,
                    role
                });
            })
            .WithName("AuthStatus")
            .WithDescription("Checks if the user is currently authenticated and returns their basic information")
            .WithSummary("Get authentication status")
            .WithOpenApi()
            .Produces<object>(StatusCodes.Status200OK);
        // .RequireAuthorization(Helper.RequireAuthenticatedUser);


        authGroup.MapPost("/login", async Task<IResult> (
                [FromBody] UserLoginRequest request,
                IJwtTokenService jwtTokenService,
                HttpContext context) =>
            {
                var jwtToken =
                    await jwtTokenService.GenerateToken(Guid.CreateVersion7().ToString(), request.Email, request.Name, "local");
                return !string.IsNullOrEmpty(jwtToken)
                    ? TypedResults.Ok(new { request.Email, Token = jwtToken })
                    : context.CreateUnauthorizedProblem("Invalid credentials provided");
            })
            .WithName("UserLogin")
            .WithDescription("Authenticates a user and returns a JWT token for subsequent requests")
            .WithSummary("Login user")
            .WithOpenApi()
            .Accepts<UserLoginRequest>("application/json")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ValidationFilter<UserLoginRequest>>();


        // User Logout
        authGroup.MapPost("/logout", async Task<IResult> (
                [FromServices] IJwtTokenService jwtTokenService,
                HttpContext context) =>
            {
                var token = jwtTokenService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return context.CreateUnauthorizedProblem("No valid authentication token found");
                }

                jwtTokenService.InvalidateToken(token);
                return TypedResults.Ok();
            })
            .WithName("UserLogout")
            .WithDescription("Invalidates the current user's JWT token")
            .WithSummary("Logout user")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

// Endpoint to start the OAuth flow
        googleAuthGroup.MapGet("/login", async Task<IResult> (
                IGoogleAuthService authService,
                HttpRequest request,
                HttpContext context) =>
            {
                // Generate the Google OAuth URL and redirect the user to it
                var authUrl = authService.GenerateOAuthUrl();
                return !string.IsNullOrEmpty(authUrl)
                    ? TypedResults.Redirect(authUrl)
                    : context.CreateNotFoundProblem("Google OAuth URL", string.Empty);
            })
            .WithName("GoogleLogin")
            .WithDescription("Initiates the Google OAuth2 authentication flow by generating an authorization URL")
            .WithSummary("Start Google authentication")
            .WithOpenApi()
            .Produces<string>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

// Endpoint to handle the OAuth callback
        googleAuthGroup.MapGet("/callback", async Task<IResult> (
                HttpRequest request,
                IGoogleAuthService authService,
                IJwtTokenService jwtService,
                UserService userService,
                HttpContext context,
                IOptions<FrontendOptions> frontendOptions,
                CancellationToken ct) =>
            {
                // Get the authorization code from the query string
                var code = request.Query["code"].ToString();
                var state = request.Query["state"].ToString();

                if (string.IsNullOrEmpty(code))
                {
                    // Redirect to frontend with error
                    var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error=no_code";
                    return TypedResults.Redirect(errorUrl);
                }

                try
                {
                    // Exchange the code for user information
                    var googleUser = await authService.HandleCallbackAsync<GoogleUserInfo>(code);

                    var newUser = OAuthUserInfo.FromUserInfoAsync(googleUser);

                    if (newUser == null)
                    {
                        var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error=user_creation_failed";
                        return TypedResults.Redirect(errorUrl);
                    }

                    var user = await userService.GetOrCreateUserFromOAuthAsync(newUser, ct);

                    // Generate your application's JWT
                    var token = await jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Name, Helper.GoogleProvider);

                    // Redirect to frontend with success and token
                    var successUrl = $"{frontendOptions.Value.GetCallbackUrl()}?token={Uri.EscapeDataString(token)}&provider=google";
                    return TypedResults.Redirect(successUrl);
                }
                catch (Exception ex)
                {
                    // Redirect to frontend with error
                    var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error={Uri.EscapeDataString(ex.Message)}";
                    return TypedResults.Redirect(errorUrl);
                }
            })
            .WithName("GoogleCallback")
            .WithDescription(
                "Handles the OAuth2 callback from Google, exchanging the authorization code for user information and generating a JWT token")
            .WithSummary("Complete google authentication")
            .WithOpenApi()
            .Produces<object>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);


// Endpoint to start the Microsoft OAuth flow
        microsoftAuthGroup.MapGet("/login", async Task<IResult> (
                IMicrosoftAuthService authService,
                HttpRequest request,
                HttpContext context) =>
            {
                // Generate the Microsoft OAuth URL for the initial authentication request
                var authUrl = authService.GenerateOAuthUrl();
                return !string.IsNullOrEmpty(authUrl)
                    ? TypedResults.Redirect(authUrl)
                    : context.CreateNotFoundProblem("Microsoft OAuth URL", string.Empty);
            })
            .WithName("MicrosoftLogin")
            .WithDescription("Initiates the Microsoft OAuth2 authentication flow by generating an authorization URL")
            .WithSummary("Start Microsoft authentication")
            .WithOpenApi()
            .Produces<string>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

// Endpoint to handle the Microsoft OAuth callback
        microsoftAuthGroup.MapGet("/callback", async Task<IResult> (
                HttpRequest request,
                IMicrosoftAuthService authService,
                IJwtTokenService jwtService,
                UserService userService,
                HttpContext context,
                IOptions<FrontendOptions> frontendOptions,
                CancellationToken ct) =>
            {
                var code = request.Query["code"].ToString();
                var state = request.Query["state"].ToString();

                if (string.IsNullOrEmpty(code))
                {
                    // Redirect to frontend with error
                    var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error=no_code";
                    return TypedResults.Redirect(errorUrl);
                }

                try
                {
                    var microsoftUser = await authService.HandleCallbackAsync<MicrosoftUserInfo>(code);

                    var authUser = OAuthUserInfo.FromUserInfoAsync(microsoftUser);

                    if (authUser == null)
                    {
                        var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error=user_creation_failed";
                        return TypedResults.Redirect(errorUrl);
                    }

                    var user = await userService.GetOrCreateUserFromOAuthAsync(authUser, ct);

                    var token = await jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Name, Helper.MicrosoftProvider);

                    // Redirect to frontend with success and token
                    var successUrl = $"{frontendOptions.Value.GetCallbackUrl()}?token={Uri.EscapeDataString(token)}&provider=microsoft";
                    return TypedResults.Redirect(successUrl);
                }
                catch (Exception ex)
                {
                    // Redirect to frontend with error
                    var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error={Uri.EscapeDataString(ex.Message)}";
                    return TypedResults.Redirect(errorUrl);
                }
            })
            .WithName("MicrosoftCallback")
            .WithDescription(
                "Handles the OAuth2 callback from Microsoft, exchanging the authorization code for user information and generating a JWT token")
            .WithSummary("Complete Microsoft authentication")
            .WithOpenApi()
            .Produces<object>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);


        facebookAuthGroup.MapGet("/login", async Task<IResult> (
                IFacebookAuthService authService,
                HttpRequest request,
                HttpContext context) =>
            {
                // Generate the Facebook OAuth URL that the user will use to authenticate
                var authUrl = authService.GenerateOAuthUrl();

                return !string.IsNullOrEmpty(authUrl)
                    ? TypedResults.Redirect(authUrl)
                    : context.CreateNotFoundProblem("Facebook OAuth URL", string.Empty);
            })
            .WithName("FacebookLogin")
            .WithDescription("Initiates the Facebook OAuth2 authentication flow by generating an authorization URL")
            .WithSummary("Start Facebook authentication")
            .WithOpenApi()
            .Produces<string>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

// Callback endpoint - Handles the OAuth response
        facebookAuthGroup.MapGet("/callback", async Task<IResult> (
                HttpRequest request,
                IFacebookAuthService authService,
                IJwtTokenService jwtService,
                UserService userService,
                HttpContext context,
                IOptions<FrontendOptions> frontendOptions,
                CancellationToken ct) =>
            {
                // Extract the authorization code from the callback
                var code = request.Query["code"].ToString();
                var state = request.Query["state"].ToString();

                if (string.IsNullOrEmpty(code))
                {
                    // Redirect to frontend with error
                    var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error=no_code";
                    return TypedResults.Redirect(errorUrl);
                }

                try
                {
                    // Exchange the authorization code for user information using the Facebook API
                    var facebookUser = await authService.HandleCallbackAsync<FacebookUserInfo>(code);

                    // Convert the Facebook-specific user info to our common OAuth format
                    var authUser = OAuthUserInfo.FromUserInfoAsync(facebookUser);

                    if (authUser == null)
                    {
                        var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error=user_creation_failed";
                        return TypedResults.Redirect(errorUrl);
                    }

                    // Create or update the user in our system
                    var user = await userService.GetOrCreateUserFromOAuthAsync(authUser, ct);

                    // Generate a JWT token for subsequent API calls
                    var token = await jwtService.GenerateToken(
                        user.Id.ToString(),
                        user.Email,
                        user.Name,
                        Helper.FacebookProvider);

                    // Redirect to frontend with success and token
                    var successUrl = $"{frontendOptions.Value.GetCallbackUrl()}?token={Uri.EscapeDataString(token)}&provider=facebook";
                    return TypedResults.Redirect(successUrl);
                }
                catch (Exception ex)
                {
                    // Redirect to frontend with error
                    var errorUrl = $"{frontendOptions.Value.GetCallbackUrl()}?error={Uri.EscapeDataString(ex.Message)}";
                    return TypedResults.Redirect(errorUrl);
                }
            })
            .WithName("FacebookCallback")
            .WithDescription(
                "Handles the OAuth2 callback from Facebook, exchanging the authorization code for user information and generating a JWT token")
            .WithSummary("Complete Facebook authentication")
            .WithOpenApi()
            .Produces<object>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}