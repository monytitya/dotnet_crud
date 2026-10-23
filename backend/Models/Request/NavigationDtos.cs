using System.ComponentModel.DataAnnotations;

namespace backend.Models.Request;

public sealed record NavigationItemResponse(int Id,string Code,string Label,string? Route,string? Icon,int SortOrder,IReadOnlyList<NavigationItemResponse> Children);
