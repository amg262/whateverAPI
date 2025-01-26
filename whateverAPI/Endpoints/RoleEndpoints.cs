using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using whateverAPI.Entities;
using whateverAPI.Helpers;
using whateverAPI.Models;
using whateverAPI.Services;

namespace whateverAPI.Endpoints;

public class RoleEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api");
        var roleGroup = apiGroup.MapGroup("/roles").WithTags("Roles");

// Get all roles
        roleGroup.MapGet("/", async Task<IResult> (
                RoleService roleService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var roles = await roleService.GetAllRolesAsync(ct);
                return roles.Count != 0
                    ? TypedResults.Ok(roles)
                    : context.CreateNotFoundProblem("Roles", "any");
            })
            .WithName("GetAllRoles")
            .WithDescription("Retrieves a complete list of all roles defined in the system")
            .WithSummary("Get all roles")
            .WithOpenApi()
            .Produces<List<Role>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization("RequireAuthenticatedUser");

// Create new role (admin only)
        roleGroup.MapPost("/", async Task<IResult> (
                [FromBody] CreateRoleRequest request,
                RoleService roleService,
                HttpContext context,
                CancellationToken ct) =>
            {
                try
                {
                    var requestingUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (requestingUserId == null || !Guid.TryParse(requestingUserId, out var adminId))
                    {
                        return context.CreateUnauthorizedProblem("Invalid user ID in token");
                    }

                    var isAdmin = await roleService.IsAdminAsync(adminId, ct);
                    if (!isAdmin)
                    {
                        return context.CreateForbiddenProblem("Only administrators can create roles");
                    }


                    var role = await roleService.CreateRoleAsync(request.Name, request.Description, ct);
                    return TypedResults.Created($"/api/roles/{role.Id}", role);
                }
                catch (InvalidOperationException ex)
                {
                    return context.CreateBadRequestProblem(ex.Message);
                }
            })
            .WithName("CreateRole")
            .WithDescription("Creates a new role in the system with specified name and optional description")
            .WithSummary("Create a new role")
            .WithOpenApi()
            .Accepts<string>("application/json")
            .Produces<Role>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<CreateRoleRequest>>()
            .RequireAuthorization("RequireAdmin");

// Assign role to user
        roleGroup.MapPut("/user/{userId:guid}/assign/{roleId:guid}", async Task<IResult> (
                Guid userId,
                Guid roleId,
                RoleService roleService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var requestingUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (requestingUserId == null || !Guid.TryParse(requestingUserId, out var adminId))
                {
                    return context.CreateUnauthorizedProblem("Invalid user ID in token");
                }

                var isAdmin = await roleService.IsAdminAsync(adminId, ct);
                if (!isAdmin)
                {
                    return context.CreateForbiddenProblem("Only administrators can assign roles");
                }

                var success = await roleService.AssignRoleToUserAsync(userId, roleId, ct);
                return success
                    ? TypedResults.Ok(new { Message = "Role assigned successfully" })
                    : context.CreateNotFoundProblem("User or Role", $"User {userId} or Role {roleId}");
            })
            .WithName("AssignUserRole")
            .WithDescription("Assigns a specified role to a user in the system")
            .WithSummary("Assign role to user")
            .WithOpenApi()
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization("RequireAdmin");


        roleGroup.MapPut("/user/{userId:guid}/assign/{roleName}", async Task<IResult> (
                AssignRoleRequest request,
                Guid userId,
                string roleName,
                RoleService roleService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var requestingUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (requestingUserId == null || !Guid.TryParse(requestingUserId, out var adminId))
                {
                    return context.CreateUnauthorizedProblem("Invalid user ID in token");
                }

                var isAdmin = await roleService.IsAdminAsync(adminId, ct);
                if (!isAdmin)
                {
                    return context.CreateForbiddenProblem("Only administrators can assign roles");
                }

                var success = await roleService.AssignRoleByNameToUserAsync(request.UserId, request.RoleName, ct);
                return success
                    ? TypedResults.Ok(new { Message = "Role assigned successfully" })
                    : context.CreateNotFoundProblem("User or Role", $"User {userId} or Role {roleName}");
            })
            .WithName("AssignUserRoleName")
            .WithDescription("Assigns a specified role by name to a user in the system")
            .WithSummary("Assign role by name to user")
            .WithOpenApi()
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<AssignRoleRequest>>()
            .RequireAuthorization("RequireAdmin");

// Get user's role
        roleGroup.MapGet("/user/{userId:guid}", async Task<IResult> (
                Guid userId,
                RoleService roleService,
                HttpContext context,
                CancellationToken ct) =>
            {
                var role = await roleService.GetUserRoleAsync(userId, ct);
                return role != null
                    ? TypedResults.Ok(role)
                    : context.CreateNotFoundProblem("User Role", userId.ToString());
            })
            .WithName("GetUserRole")
            .WithDescription("Retrieves the current role assigned to a specific user")
            .WithSummary("Get user's role")
            .WithOpenApi()
            .Produces<Role>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization("RequireAuthenticatedUser");
    }
}