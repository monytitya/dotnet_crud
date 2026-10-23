using backend.Data;
using backend.Models.Data;
using backend.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class RoleService(AppDbContext db) : IRoleService
{
    public async Task<RolePageResponse> GetPageAsync(RoleQueryRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status.Equals("active", StringComparison.OrdinalIgnoreCase)) query = query.Where(role => role.IsActive);
            else if (request.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase)) query = query.Where(role => !role.IsActive);
            else return new([], pageNumber, pageSize, 0, 0, 0, 0);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(role => role.Code.Contains(term) || role.Name.Contains(term) ||
                                        (role.Description != null && role.Description.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(role => role.Name)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(role => new RoleResponse(role.Id, role.Code, role.Name, role.Description,
                role.IsActive, role.CreatedAtUtc, role.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        var active = await query.CountAsync(role => role.IsActive, cancellationToken);
        return new(rows, pageNumber, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize), total, active);
    }

    public async Task<RoleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().Where(role => role.Id == id)
            .Select(role => new RoleResponse(role.Id, role.Code, role.Name, role.Description,
                role.IsActive, role.CreatedAtUtc, role.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<RoleServiceResult<RoleResponse>> CreateAsync(
        CreateRoleRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Roles.AnyAsync(role => role.Code == code, cancellationToken))
            return new(null, RoleServiceError.DuplicateCode, "A role with this code already exists.");
        var role = new AppRole
        {
            Code = code, Name = request.Name.Trim(), Description = request.Description?.Trim(), CreatedBy = actorUserId
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return new(new RoleResponse(role.Id, role.Code, role.Name, role.Description,
            role.IsActive, role.CreatedAtUtc, role.UpdatedAtUtc), RoleServiceError.None, null);
    }

    public async Task<RoleServiceResult<RoleResponse>> UpdateAsync(
        int id, UpdateRoleRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FindAsync([id], cancellationToken);
        if (role is null) return new(null, RoleServiceError.NotFound, "Role not found.");
        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        role.IsActive = request.IsActive;
        role.UpdatedBy = actorUserId;
        role.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new(new RoleResponse(role.Id, role.Code, role.Name, role.Description,
            role.IsActive, role.CreatedAtUtc, role.UpdatedAtUtc), RoleServiceError.None, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FindAsync([id], cancellationToken);
        if (role is null) return false;
        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPermissionsAsync(
        int id, int[] permissionIds, int actorUserId, CancellationToken cancellationToken)
    {
        var role = await db.Roles.Include(item => item.RolePermissions)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (role is null) return false;
        var distinctIds = permissionIds.Distinct().ToArray();
        var validIds = await db.Permissions.Where(permission => distinctIds.Contains(permission.Id))
            .Select(permission => permission.Id).ToListAsync(cancellationToken);
        if (validIds.Count != distinctIds.Length) return false;
        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permissionId in validIds)
            role.RolePermissions.Add(new AppRolePermission { RoleId = id, PermissionId = permissionId, AssignedBy = actorUserId });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
