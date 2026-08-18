namespace SRMShared.Entities;

public class MaintenanceWindow : BaseEntity
{
    public Guid ServerRoomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Description { get; set; } = string.Empty;

    public ServerRoom? ServerRoom { get; set; }
}
