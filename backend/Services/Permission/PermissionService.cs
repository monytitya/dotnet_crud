using backend.Data;
using backend.Models.Data;
using backend.Models.Request;
using backend.Security;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Permission;

public sealed class PermissionService(AppDbContext db) : IPermissionService
{
    public async Task<IReadOnlyList<PermissionResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking()
            .OrderBy(permission => permission.Page.Name)
            .ThenBy(permission => permission.Action)
            .Select(permission => new PermissionResponse(permission.Id, permission.PageId,
                permission.Page.Code, permission.Page.Name, permission.Code, permission.Action,
                permission.Description, permission.IsActive, permission.CreatedAtUtc, permission.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<PermissionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking()
            .Where(permission => permission.Id == id)
            .Select(permission => new PermissionResponse(permission.Id, permission.PageId,
                permission.Page.Code, permission.Page.Name, permission.Code, permission.Action,
                permission.Description, permission.IsActive, permission.CreatedAtUtc, permission.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PermissionServiceResult<PermissionResponse>> CreateAsync(
        CreatePermissionRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        if (!await db.Pages.AnyAsync(page => page.Id == request.PageId, cancellationToken))
            return new(null, PermissionServiceError.InvalidPage, "The selected page does not exist.");
        var action = request.Action.Trim().ToUpperInvariant();
        if (!AppPermissionActions.IsValid(action))
            return new(null, PermissionServiceError.InvalidAction, "The permission action is not supported.");
        var code = $"{request.PageId}:{action}";
        if (await db.Permissions.AnyAsync(permission => permission.Code == code, cancellationToken))
            return new(null, PermissionServiceError.InvalidAction, "This page already has that permission action.");

        var permission = new AppPermission
        {
            PageId = request.PageId, Action = action, Code = code,
            Description = request.Description?.Trim(), CreatedBy = actorUserId
        };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(cancellationToken);
        return new(await GetByIdAsync(permission.Id, cancellationToken), PermissionServiceError.None, null);
    }

    public async Task<PermissionServiceResult<PermissionResponse>> UpdateAsync(
        int id, UpdatePermissionRequest request, int actorUserId, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.FindAsync([id], cancellationToken);
        if (permission is null)
            return new(null, PermissionServiceError.NotFound, "Permission not found.");
        permission.Description = request.Description?.Trim();
        permission.IsActive = request.IsActive;
        permission.UpdatedBy = actorUserId;
        permission.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new(await GetByIdAsync(id, cancellationToken), PermissionServiceError.None, null);
    }

}
