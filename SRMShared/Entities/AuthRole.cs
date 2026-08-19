namespace SRMShared.Entities;

public class AuthRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();
}
