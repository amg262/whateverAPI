using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using whateverAPI.Helpers;
using whateverAPI.Models;
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
                HttpResponse response,
                HttpContext context) =>
            {
                // Generate the Google OAuth URL and redirect the user to it
                var authUrl = authService.GenerateOAuthUrl();
                return !string.IsNullOrEmpty(authUrl)
                    ? TypedResults.Ok(authUrl)
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
                CancellationToken ct) =>
            {
                // Get the authorization code from the query string
                var code = request.Query["code"].ToString();

                if (string.IsNullOrEmpty(code))
                {
                    return context.CreateNotFoundProblem("Authorization code", string.Empty);
                    return TypedResults.BadRequest("No authorization code provided");
                }

                try
                {
                    // Exchange the code for user information
                    var googleUser = await authService.HandleCallbackAsync<GoogleUserInfo>(code);

                    var newUser = OAuthUserInfo.FromUserInfoAsync(googleUser);

                    if (newUser == null)
                    {
                        return context.CreateBadRequestProblem("User account be created");
                    }

                    var user = await userService.GetOrCreateUserFromOAuthAsync(newUser, ct);

                    // Generate your application's JWT
                    var token = jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Name, Helper.GoogleProvider);

                    // Return the user information and token
                    return TypedResults.Ok(new
                    {
                        token,
                        user
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("GoogleCallback")
            .WithDescription(
                "Handles the OAuth2 callback from Google, exchanging the authorization code for user information and generating a JWT token")
            .WithSummary("Complete google authentication")
            .WithOpenApi()
            .Produces<GoogleUserInfo>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);


// Endpoint to start the Microsoft OAuth flow
        microsoftAuthGroup.MapGet("/login", async Task<IResult> (
                IMicrosoftAuthService authService,
                HttpResponse response,
                HttpContext context) =>
            {
                // Generate the Microsoft OAuth URL for the initial authentication request
                var authUrl = authService.GenerateOAuthUrl();
                return !string.IsNullOrEmpty(authUrl)
                    ? TypedResults.Ok(authUrl)
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
                HttpContext context) =>
            {
                var code = request.Query["code"].ToString();

                if (string.IsNullOrEmpty(code))
                {
                    return context.CreateNotFoundProblem("Authorization code", string.Empty);
                    return TypedResults.BadRequest("No authorization code provided");
                }

                try
                {
                    var microsoftUser = await authService.HandleCallbackAsync<MicrosoftUserInfo>(code);

                    var authUser = OAuthUserInfo.FromUserInfoAsync(microsoftUser);

                    // var newUser = OAuthUserInfo.FromMicrosoftUserInfo(microsoftUser);

                    if (authUser == null)
                    {
                        return context.CreateBadRequestProblem("User account cannot be created");
                    }

                    var user = await userService.GetOrCreateUserFromOAuthAsync(authUser);

                    var token = jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Name, Helper.GoogleProvider);


                    return TypedResults.Ok(new
                    {
                        token,
                        user
                    });
                }
                catch (Exception ex)
                {
                    return context.CreateBadRequestProblem(ex.Message);
                    return TypedResults.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("MicrosoftCallback")
            .WithDescription(
                "Handles the OAuth2 callback from Microsoft, exchanging the authorization code for user information and generating a JWT token")
            .WithSummary("Complete Microsoft authentication")
            .WithOpenApi()
            .Produces<MicrosoftUserInfo>(StatusCodes.Status200OK, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);


        facebookAuthGroup.MapGet("/login", async Task<IResult> (
                IFacebookAuthService authService,
                HttpResponse response,
                HttpContext context) =>
            {
                // Generate the Facebook OAuth URL that the user will use to authenticate
                var authUrl = authService.GenerateOAuthUrl();

                return !string.IsNullOrEmpty(authUrl)
                    ? TypedResults.Ok(authUrl)
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
                CancellationToken ct) =>
            {
                // Extract the authorization code from the callback
                var code = request.Query["code"].ToString();

                if (string.IsNullOrEmpty(code))
                {
                    return context.CreateNotFoundProblem("Authorization code", string.Empty);
                }

                try
                {
                    // Exchange the authorization code for user information using the Facebook API
                    var facebookUser = await authService.HandleCallbackAsync<FacebookUserInfo>(code);

                    // Convert the Facebook-specific user info to our common OAuth format
                    var authUser = OAuthUserInfo.FromUserInfoAsync(facebookUser);

                    if (authUser == null)
                    {
                        return context.CreateBadRequestProblem("User account cannot be created");
                    }

                    // Create or update the user in our system
                    var user = await userService.GetOrCreateUserFromOAuthAsync(authUser, ct);

                    // Generate a JWT token for subsequent API calls
                    var token = jwtService.GenerateToken(
                        user.Id.ToString(),
                        user.Email,
                        user.Name,
                        Helper.FacebookProvider);

                    // Return both the user information and the JWT token
                    return TypedResults.Ok(new
                    {
                        token,
                        user
                    });
                }
                catch (Exception ex)
                {
                    return context.CreateExternalServiceProblem(
                        "Facebook Authentication",
                        "Failed to process Facebook authentication",
                        ex);
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