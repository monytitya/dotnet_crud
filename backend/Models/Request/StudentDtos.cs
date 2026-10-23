using System.ComponentModel.DataAnnotations;

namespace backend.Models.Request;

public sealed record StudentRequest(
    [Required, StringLength(200)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email);

public sealed record StudentResponse(int Id, string Name, string Email);
