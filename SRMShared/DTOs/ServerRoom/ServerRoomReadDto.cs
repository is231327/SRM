namespace SRMShared.DTOs.ServerRoom;

public class ServerRoomReadDto : ServerRoomBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
