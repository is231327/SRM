using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class CrudService<TEntity>(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : ICrudService<TEntity>
    where TEntity : BaseEntity
{
    protected readonly SrmCoreDbContext DbContext = dbContext;
    protected readonly ICurrentUserContext CurrentUserContext = currentUserContext;

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await ApplyOwnershipFilter(DbContext.Set<TEntity>()).ToListAsync();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await ApplyOwnershipFilter(DbContext.Set<TEntity>()).FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        await EnsureCanWriteAsync(entity);
        entity.Id = Guid.NewGuid();
        DbContext.Set<TEntity>().Add(entity);
        await DbContext.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TEntity?> UpdateAsync(Guid id, TEntity entity)
    {
        TEntity? existing = await ApplyOwnershipFilter(DbContext.Set<TEntity>()).FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return null;
        }

        await EnsureCanWriteAsync(entity);
        entity.Id = id;
        entity.CreatedAtUtc = existing.CreatedAtUtc;
        entity.UpdatedAtUtc = existing.UpdatedAtUtc;
        DbContext.Entry(existing).CurrentValues.SetValues(entity);
        await DbContext.SaveChangesAsync();
        return existing;
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        TEntity? existing = await ApplyOwnershipFilter(DbContext.Set<TEntity>()).FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return false;
        }

        DbContext.Set<TEntity>().Remove(existing);
        await DbContext.SaveChangesAsync();
        return true;
    }

    protected virtual IQueryable<TEntity> ApplyOwnershipFilter(IQueryable<TEntity> query)
    {
        if (!CurrentUserContext.IsCustomerScopedUser)
        {
            return query;
        }

        var customerId = CurrentUserContext.CustomerId
            ?? throw new ForbiddenAccessException("Customer-scoped users require a customer claim.");

        return typeof(TEntity).Name switch
        {
            nameof(ServerRoom) => (IQueryable<TEntity>)DbContext.ServerRooms.Where(x => x.CustomerId == customerId),
            nameof(Agent) => (IQueryable<TEntity>)DbContext.Agents.Where(x => x.ServerRoom != null && x.ServerRoom.CustomerId == customerId),
            nameof(ShellyDevice) => (IQueryable<TEntity>)DbContext.ShellyDevices.Where(x => x.Agent != null && x.Agent.ServerRoom != null && x.Agent.ServerRoom.CustomerId == customerId),
            nameof(MonitoredDevice) => (IQueryable<TEntity>)DbContext.MonitoredDevices.Where(x => x.Agent != null && x.Agent.ServerRoom != null && x.Agent.ServerRoom.CustomerId == customerId),
            nameof(MonitoredDevicePingResult) => (IQueryable<TEntity>)DbContext.MonitoredDevicePingResults.Where(x => x.MonitoredDevice != null && x.MonitoredDevice.Agent != null && x.MonitoredDevice.Agent.ServerRoom != null && x.MonitoredDevice.Agent.ServerRoom.CustomerId == customerId),
            nameof(MaintenanceWindow) => (IQueryable<TEntity>)DbContext.MaintenanceWindows.Where(x => x.ServerRoom != null && x.ServerRoom.CustomerId == customerId),
            nameof(SensorReading) => (IQueryable<TEntity>)DbContext.SensorReadings.Where(x => x.ShellyDevice != null && x.ShellyDevice.Agent != null && x.ShellyDevice.Agent.ServerRoom != null && x.ShellyDevice.Agent.ServerRoom.CustomerId == customerId),
            _ => query
        };
    }

    protected virtual async Task EnsureCanWriteAsync(TEntity entity)
    {
        if (!CurrentUserContext.IsCustomerScopedUser)
        {
            return;
        }

        var customerId = CurrentUserContext.CustomerId
            ?? throw new ForbiddenAccessException("Customer-scoped users require a customer claim.");

        var isAllowed = entity switch
        {
            ServerRoom serverRoom => serverRoom.CustomerId == customerId,
            Agent agent => await DbContext.ServerRooms.AnyAsync(x => x.Id == agent.ServerRoomId && x.CustomerId == customerId),
            ShellyDevice shellyDevice => await DbContext.Agents.AnyAsync(x => x.Id == shellyDevice.AgentId && x.ServerRoom != null && x.ServerRoom.CustomerId == customerId),
            MonitoredDevice monitoredDevice => await DbContext.Agents.AnyAsync(x => x.Id == monitoredDevice.AgentId && x.ServerRoom != null && x.ServerRoom.CustomerId == customerId),
            MonitoredDevicePingResult pingResult => await DbContext.MonitoredDevices.AnyAsync(x => x.Id == pingResult.MonitoredDeviceId && x.Agent != null && x.Agent.ServerRoom != null && x.Agent.ServerRoom.CustomerId == customerId),
            MaintenanceWindow maintenanceWindow => await DbContext.ServerRooms.AnyAsync(x => x.Id == maintenanceWindow.ServerRoomId && x.CustomerId == customerId),
            SensorReading sensorReading => await DbContext.ShellyDevices.AnyAsync(x => x.Id == sensorReading.ShellyDeviceId && x.Agent != null && x.Agent.ServerRoom != null && x.Agent.ServerRoom.CustomerId == customerId),
            _ => false
        };

        if (!isAllowed)
        {
            throw new ForbiddenAccessException("The current user is not allowed to access the target customer data.");
        }
    }
}
