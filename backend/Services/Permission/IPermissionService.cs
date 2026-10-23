using backend.Models.Request;

namespace backend.Services.Permission;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<PermissionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PermissionServiceResult<PermissionResponse>> CreateAsync(CreatePermissionRequest request, int actorUserId, CancellationToken cancellationToken);
    Task<PermissionServiceResult<PermissionResponse>> UpdateAsync(int id, UpdatePermissionRequest request, int actorUserId, CancellationToken cancellationToken);
}

public enum PermissionServiceError { None, InvalidPage, InvalidAction, NotFound }

public sealed record PermissionServiceResult<T>(T? Value, PermissionServiceError Error, string? Message)
{
    public bool Succeeded => Error == PermissionServiceError.None;
}
