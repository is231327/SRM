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
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<TicketLink> TicketLinks => Set<TicketLink>();

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

        modelBuilder.Entity<ServerRoom>()
            .HasMany(serverRoom => serverRoom.Incidents)
            .WithOne(incident => incident.ServerRoom)
            .HasForeignKey(incident => incident.ServerRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShellyDevice>()
            .HasMany(shellyDevice => shellyDevice.Incidents)
            .WithOne(incident => incident.ShellyDevice)
            .HasForeignKey(incident => incident.ShellyDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MonitoredDevice>()
            .HasMany(monitoredDevice => monitoredDevice.Incidents)
            .WithOne(incident => incident.MonitoredDevice)
            .HasForeignKey(incident => incident.MonitoredDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.Type)
            .HasConversion<int>();

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.Severity)
            .HasConversion<int>();

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Incident>()
            .Property(incident => incident.CorrelationKey)
            .HasMaxLength(256);

        modelBuilder.Entity<Incident>()
            .HasIndex(incident => new { incident.CorrelationKey, incident.Status });

        modelBuilder.Entity<Incident>()
            .HasMany(incident => incident.Events)
            .WithOne(incidentEvent => incidentEvent.Incident)
            .HasForeignKey(incidentEvent => incidentEvent.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Incident>()
            .HasMany(incident => incident.TicketLinks)
            .WithOne(ticketLink => ticketLink.Incident)
            .HasForeignKey(ticketLink => ticketLink.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketLink>()
            .Property(ticketLink => ticketLink.SyncStatus)
            .HasConversion<int>();

        modelBuilder.Entity<TicketLink>()
            .Property(ticketLink => ticketLink.ProviderName)
            .HasMaxLength(64);

        modelBuilder.Entity<TicketLink>()
            .Property(ticketLink => ticketLink.ExternalStatusName)
            .HasMaxLength(64);

        modelBuilder.Entity<TicketLink>()
            .Property(ticketLink => ticketLink.ExternalPriorityName)
            .HasMaxLength(64);

        modelBuilder.Entity<TicketLink>()
            .HasIndex(ticketLink => new { ticketLink.IncidentId, ticketLink.ProviderName })
            .IsUnique();
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
