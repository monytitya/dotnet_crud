namespace backend.Models.Data;
public sealed class AuthAuditLog { public int Id{get;set;} public int? UserId{get;set;} public string? Username{get;set;} public required string EventType{get;set;} public bool Success{get;set;} public string? IpAddress{get;set;} public string? Details{get;set;} public DateTimeOffset CreatedAtUtc{get;set;}=DateTimeOffset.UtcNow; }
