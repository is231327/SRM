using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface IIncidentService
{
    Task EvaluateSensorReadingAsync(SensorReading sensorReading, CancellationToken cancellationToken = default);
    Task EvaluatePingResultAsync(MonitoredDevicePingResult pingResult, CancellationToken cancellationToken = default);
}
