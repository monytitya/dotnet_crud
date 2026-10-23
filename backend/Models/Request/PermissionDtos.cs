using System.ComponentModel.DataAnnotations;

namespace backend.Models.Request;

public sealed class CreatePermissionRequest
{
    [Range(1, int.MaxValue)] public int PageId { get; set; }
    [Required, StringLength(30)] public required string Action { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdatePermissionRequest
{
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed record PermissionResponse(int Id,int PageId,string PageCode,string PageName,string Code,string Action,string? Description,bool IsActive,DateTimeOffset CreatedAtUtc,DateTimeOffset? UpdatedAtUtc);
