using backend.Models.Request;

namespace backend.Services;

public interface IRoleService
{
    Task<RolePageResponse> GetPageAsync(RoleQueryRequest request, CancellationToken cancellationToken);
    Task<RoleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<RoleServiceResult<RoleResponse>> CreateAsync(CreateRoleRequest request, int actorUserId, CancellationToken cancellationToken);
    Task<RoleServiceResult<RoleResponse>> UpdateAsync(int id, UpdateRoleRequest request, int actorUserId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<bool> SetPermissionsAsync(int id, int[] permissionIds, int actorUserId, CancellationToken cancellationToken);
}

public enum RoleServiceError { None, InvalidStatus, DuplicateCode, NotFound }

public sealed record RoleServiceResult<T>(T? Value, RoleServiceError Error, string? Message)
{
    public bool Succeeded => Error == RoleServiceError.None;
}
