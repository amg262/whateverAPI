using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Services;

namespace whateverAPI.Endpoints;

public class UserEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");
        var userGroup2 = apiGroup.MapGroup("/user").WithTags("User");
        
        var userGroup = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/user")
            .WithTags("User")
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(Helper.GlobalPolicy);
        

        // User Login
        userGroup.MapPost("/login", async Task<IResult> (
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
        userGroup.MapPost("/logout", async Task<IResult> (
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
    }
}