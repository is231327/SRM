using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.Agent;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class AgentDtoMapper : ICrudDtoMapper<Agent, AgentCreateDto, AgentUpdateDto, AgentReadDto>
{
    public AgentReadDto ToReadDto(Agent entity) => new()
    {
        Id = entity.Id,
        ServerRoomId = entity.ServerRoomId,
        Name = entity.Name,
        ApiKeyReference = entity.ApiKeyReference,
        Version = entity.Version,
        LastKnownIpAddress = entity.LastKnownIpAddress,
        LastSeenAtUtc = entity.LastSeenAtUtc,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public Agent ToEntity(AgentCreateDto dto) => new()
    {
        ServerRoomId = dto.ServerRoomId,
        Name = dto.Name,
        ApiKeyReference = dto.ApiKeyReference,
        Version = dto.Version,
        LastKnownIpAddress = dto.LastKnownIpAddress,
        LastSeenAtUtc = dto.LastSeenAtUtc,
        IsActive = dto.IsActive
    };

    public Agent ToEntity(AgentUpdateDto dto) => new()
    {
        ServerRoomId = dto.ServerRoomId,
        Name = dto.Name,
        ApiKeyReference = dto.ApiKeyReference,
        Version = dto.Version,
        LastKnownIpAddress = dto.LastKnownIpAddress,
        LastSeenAtUtc = dto.LastSeenAtUtc,
        IsActive = dto.IsActive
    };
}
