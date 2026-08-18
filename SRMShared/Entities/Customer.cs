namespace SRMShared.Entities;

public class Customer : BaseEntity
{
    public string ExternalReference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<ServerRoom> ServerRooms { get; set; } = new List<ServerRoom>();
}
