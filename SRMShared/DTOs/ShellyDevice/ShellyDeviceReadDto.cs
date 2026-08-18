namespace SRMShared.DTOs.ShellyDevice;

public class ShellyDeviceReadDto : ShellyDeviceBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
