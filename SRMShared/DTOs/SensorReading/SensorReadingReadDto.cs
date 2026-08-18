namespace SRMShared.DTOs.SensorReading;

public class SensorReadingReadDto : SensorReadingBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
