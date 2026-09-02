namespace SRMShared.Entities;

public class AuthUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public bool MfaEnabled { get; set; }
    public string MfaSecretProtected { get; set; } = string.Empty;
    public long? MfaLastUsedTimeStep { get; set; }

    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();
    public ICollection<CustomerUser> CustomerUsers { get; set; } = new List<CustomerUser>();
    public ICollection<MfaRecoveryCode> MfaRecoveryCodes { get; set; } = new List<MfaRecoveryCode>();
}
