namespace SRMShared.Entities;

public enum IncidentType
{
    DoorOpenOutsideMaintenanceWindow = 1,
    TemperatureWarningThresholdExceeded = 2,
    TemperatureCriticalThresholdExceeded = 3,
    MonitoredDeviceFailureThresholdReached = 4
}
