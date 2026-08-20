namespace SRMShared.Entities;

public class ServerRoom : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LocationDescription { get; set; } = string.Empty;
    public float TemperatureWarningThreshold { get; set; }
    public float TemperatureCriticalThreshold { get; set; }
    public bool MonitoringEnabled { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<MaintenanceWindow> MaintenanceWindows { get; set; } = new List<MaintenanceWindow>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
