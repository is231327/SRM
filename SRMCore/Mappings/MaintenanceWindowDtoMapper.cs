using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class MaintenanceWindowDtoMapper : ICrudDtoMapper<MaintenanceWindow, MaintenanceWindowCreateDto, MaintenanceWindowUpdateDto, MaintenanceWindowReadDto>
{
    public MaintenanceWindowReadDto ToReadDto(MaintenanceWindow entity) => new()
    {
        Id = entity.Id,
        ServerRoomId = entity.ServerRoomId,
        Title = entity.Title,
        StartUtc = entity.StartUtc,
        EndUtc = entity.EndUtc,
        Description = entity.Description,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public MaintenanceWindow ToEntity(MaintenanceWindowCreateDto dto) => new()
    {
        ServerRoomId = dto.ServerRoomId,
        Title = dto.Title,
        StartUtc = dto.StartUtc,
        EndUtc = dto.EndUtc,
        Description = dto.Description
    };

    public MaintenanceWindow ToEntity(MaintenanceWindowUpdateDto dto) => new()
    {
        ServerRoomId = dto.ServerRoomId,
        Title = dto.Title,
        StartUtc = dto.StartUtc,
        EndUtc = dto.EndUtc,
        Description = dto.Description
    };
}
