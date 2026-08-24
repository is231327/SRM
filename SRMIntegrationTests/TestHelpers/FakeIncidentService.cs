using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMIntegrationTests.TestHelpers;

public class FakeIncidentService : IIncidentService
{
    public Task EvaluateSensorReadingAsync(SensorReading sensorReading, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task EvaluatePingResultAsync(MonitoredDevicePingResult pingResult, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
