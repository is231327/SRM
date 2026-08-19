namespace SRMShared.Entities;

public class AuthUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AuthUser? User { get; set; }
    public AuthRole? Role { get; set; }
}
