namespace SRMShared.DTOs.Customer;

public class CustomerReadDto : CustomerBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
