using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.Agent;
using SRMShared.DTOs.AgentRuntime;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.ShellyDevice;
using SRMShared.Entities;

namespace SRMCore.Services;

public class AgentRuntimeService(
    SrmCoreDbContext dbContext,
    ICrudDtoMapper<Agent, AgentCreateDto, AgentUpdateDto, AgentReadDto> agentMapper,
    ICrudDtoMapper<ShellyDevice, ShellyDeviceCreateDto, ShellyDeviceUpdateDto, ShellyDeviceReadDto> shellyMapper,
    ICrudDtoMapper<MonitoredDevice, MonitoredDeviceCreateDto, MonitoredDeviceUpdateDto, MonitoredDeviceReadDto> monitoredDeviceMapper) : IAgentRuntimeService
{
    public async Task<AgentRuntimeConfigurationDto?> GetRuntimeConfigurationAsync()
    {
       
        var agentId = dbContext.Agents.FirstOrDefault().Id;

        var agent = await dbContext.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == agentId && x.IsActive);

        if (agent is null)
        {
            return null;
        }

        var shellyDevices = await dbContext.ShellyDevices
            .AsNoTracking()
            .Where(x => x.AgentId == agentId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var monitoredDevices = await dbContext.MonitoredDevices
            .AsNoTracking()
            .Where(x => x.AgentId == agentId && x.IsActive)
            .OrderBy(x => x.DisplayName)
            .ToListAsync();

        return new AgentRuntimeConfigurationDto
        {
            Agent = agentMapper.ToReadDto(agent),
            ShellyDevices = shellyDevices.Select(shellyMapper.ToReadDto).ToArray(),
            MonitoredDevices = monitoredDevices.Select(monitoredDeviceMapper.ToReadDto).ToArray()
        };
    }
}
