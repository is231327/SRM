using Microsoft.EntityFrameworkCore;
using SRMShared.Entities;

namespace SRMCore.Data;

public class SrmCoreDbContext(DbContextOptions<SrmCoreDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServerRoom> ServerRooms => Set<ServerRoom>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<ShellyDevice> ShellyDevices => Set<ShellyDevice>();
    public DbSet<MonitoredDevice> MonitoredDevices => Set<MonitoredDevice>();
    public DbSet<MonitoredDevicePingResult> MonitoredDevicePingResults => Set<MonitoredDevicePingResult>();
    public DbSet<MaintenanceWindow> MaintenanceWindows => Set<MaintenanceWindow>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasMany(customer => customer.ServerRooms)
            .WithOne(serverRoom => serverRoom.Customer)
            .HasForeignKey(serverRoom => serverRoom.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServerRoom>()
            .HasMany(serverRoom => serverRoom.Agents)
            .WithOne(agent => agent.ServerRoom)
            .HasForeignKey(agent => agent.ServerRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServerRoom>()
            .HasMany(serverRoom => serverRoom.MaintenanceWindows)
            .WithOne(maintenanceWindow => maintenanceWindow.ServerRoom)
            .HasForeignKey(maintenanceWindow => maintenanceWindow.ServerRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Agent>()
            .HasMany(agent => agent.ShellyDevices)
            .WithOne(shellyDevice => shellyDevice.Agent)
            .HasForeignKey(shellyDevice => shellyDevice.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Agent>()
            .HasMany(agent => agent.MonitoredDevices)
            .WithOne(monitoredDevice => monitoredDevice.Agent)
            .HasForeignKey(monitoredDevice => monitoredDevice.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MonitoredDevice>()
            .HasMany(monitoredDevice => monitoredDevice.PingResults)
            .WithOne(pingResult => pingResult.MonitoredDevice)
            .HasForeignKey(pingResult => pingResult.MonitoredDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShellyDevice>()
            .HasMany(shellyDevice => shellyDevice.SensorReadings)
            .WithOne(sensorReading => sensorReading.ShellyDevice)
            .HasForeignKey(sensorReading => sensorReading.ShellyDeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        DateTime now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
