using System.ComponentModel.DataAnnotations;

namespace backend.Models.Request;

public sealed class RoleQueryRequest
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [RegularExpression("^(?i)(active|inactive)$")] public string? Status { get; set; }
    public string? Search { get; set; }
}

public sealed class CreateRoleRequest
{
    [Required, StringLength(50)] public required string Code { get; set; }
    [Required, StringLength(100)] public required string Name { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateRoleRequest
{
    [Required, StringLength(100)] public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
public sealed record RoleResponse(int Id,string Code,string Name,string? Description,bool IsActive,DateTimeOffset CreatedAtUtc,DateTimeOffset? UpdatedAtUtc);
public sealed record RolePageResponse(IReadOnlyList<RoleResponse> Items,int PageNumber,int PageSize,int TotalCount,int TotalPages,int TotalRoles,int ActiveRoles);
