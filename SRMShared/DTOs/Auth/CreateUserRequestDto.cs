using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class CreateUserRequestDto
{
    [Required]
    [StringLength(200)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<string> Roles { get; set; } = [];

    public Guid? CustomerId { get; set; }
}
