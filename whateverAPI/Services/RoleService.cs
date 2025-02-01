using Microsoft.EntityFrameworkCore;
using whateverAPI.Data;
using whateverAPI.Entities;
using whateverAPI.Helpers;

namespace whateverAPI.Services;

public class RoleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RoleService> _logger;

    public RoleService(AppDbContext db, ILogger<RoleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Role> CreateRoleAsync(string name, string? description = null, CancellationToken ct = default)
    {
        // Check if role already exists
        var existingRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower(), ct);

        if (existingRole != null)
        {
            throw new InvalidOperationException($"Role '{name}' already exists");
        }

        var role = Role.Create(name, description);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created new role: {RoleName}", name);
        return role;
    }

    public async Task<bool> AssignRoleByNameToUserAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var role = _db.Roles.FirstOrDefault(r => r.Name.ToLower().Trim() == roleName.ToLower().Trim());

        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' does not exist");
        }

        var user = await _db.Users.FindAsync([userId], ct);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' does not exist");
        }

        user.RoleId = role.Id;
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user == null)
        {
            _logger.LogWarning("Attempted to assign role to non-existent user {UserId}", userId);
            return false;
        }

        var role = await _db.Roles.FindAsync([roleId], ct);
        if (role == null)
        {
            _logger.LogWarning("Attempted to assign non-existent role {RoleId}", roleId);
            return false;
        }

        user.RoleId = roleId;
        user.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Assigned role {RoleName} to user {UserId}", role.Name, userId);
        return true;
    }

    public async Task<Role?> GetUserRoleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user?.Role;
    }

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken ct = default)
    {
        return await _db.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    private async Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken ct = default) =>
        await _db.Users.AnyAsync(u => u.Id == userId && u.Role!.Name.ToLower() == roleName.ToLower(), ct);


    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default) =>
        await HasRoleAsync(userId, Helper.AdminRole, ct);


    public async Task<bool> IsModeratorOrAboveAsync(Guid userId, CancellationToken ct = default)
    {
        var allowedRoles = new[] { Helper.AdminRole, Helper.ModeratorRole };
        return await _db.Users.AnyAsync(u => u.Id == userId && allowedRoles.Contains(u.Role!.Name.ToLower()), ct);
    }
}