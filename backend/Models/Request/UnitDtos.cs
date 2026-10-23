using System.ComponentModel.DataAnnotations;

namespace backend.Models.Request;

public sealed class UnitQueryRequest
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [RegularExpression("^(?i)(active|inactive)$")] public string? Status { get; set; }
    [RegularExpression("^(?i)(Weight|Volume|Count)$")] public string? Group { get; set; }
    public string? Search { get; set; }
}

public sealed class CreateUnitRequest
{
    [Required, StringLength(100)] public required string Name { get; set; }
    [Required, StringLength(20)] public required string Symbol { get; set; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999")] public decimal Measure { get; set; }
    [Required] public required string Group { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateUnitRequest
{
    [Required, StringLength(100)] public required string Name { get; set; }
    [Required, StringLength(20)] public required string Symbol { get; set; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999")] public decimal Measure { get; set; }
    [Required] public required string Group { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
public sealed record UnitResponse(int Id,string Name,string Symbol,decimal Measure,string Group,string? Description,bool IsActive,DateTimeOffset CreatedAtUtc,DateTimeOffset? UpdatedAtUtc);
public sealed record UnitPageResponse(IReadOnlyList<UnitResponse> Items,int PageNumber,int PageSize,int TotalCount,int TotalPages,int TotalUnits,int ActiveUnits,int GroupCount);
