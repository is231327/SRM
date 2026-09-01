namespace SRMShared.Entities;

public class CustomerUser : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CustomerId { get; set; }

    public AuthUser? User { get; set; }
}
