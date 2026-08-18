namespace SRMShared.DTOs.MaintenanceWindow;

public class MaintenanceWindowReadDto : MaintenanceWindowBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
