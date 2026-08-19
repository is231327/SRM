using NUnit.Framework;
using SRMShared.DTOs.Agent;
using SRMShared.DTOs.Customer;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.SensorReading;
using SRMShared.DTOs.ServerRoom;
using SRMShared.DTOs.ShellyDevice;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.DTOs;

[TestFixture]
public class DtoValidationTests
{
    [Test]
    public void CustomerCreateDto_ShouldFailForInvalidEmail()
    {
        var dto = new CustomerCreateDto
        {
            ExternalReference = "CUS-001",
            Name = "Customer A",
            ContactEmail = "not-an-email",
            ContactPhone = "123",
            IsActive = true
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(CustomerBaseDto.ContactEmail))), Is.True);
    }

    [Test]
    public void ServerRoomCreateDto_ShouldFailWhenCriticalThresholdIsNotGreaterThanWarningThreshold()
    {
        var dto = new ServerRoomCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Name = "Room A",
            LocationDescription = "First floor",
            TemperatureWarningThreshold = 30,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(ServerRoomBaseDto.TemperatureCriticalThreshold))), Is.True);
    }

    [Test]
    public void AgentCreateDto_ShouldFailForInvalidIpAddress()
    {
        var dto = new AgentCreateDto
        {
            ServerRoomId = Guid.NewGuid(),
            Name = "Agent A",
            ApiKeyReference = "key-a",
            Version = "1.0.0",
            LastKnownIpAddress = "invalid-ip",
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(AgentBaseDto.LastKnownIpAddress))), Is.True);
    }

    [Test]
    public void ShellyDeviceCreateDto_ShouldFailForInvalidBaseUrlAndMacAddress()
    {
        var dto = new ShellyDeviceCreateDto
        {
            AgentId = Guid.NewGuid(),
            Name = "Shelly A",
            DeviceType = "DoorWindow2",
            BaseUrl = "not-a-url",
            MacAddress = "invalid-mac",
            FirmwareVersion = "1.0",
            IsVirtual = true,
            IsActive = true
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(ShellyDeviceBaseDto.BaseUrl))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(ShellyDeviceBaseDto.MacAddress))), Is.True);
    }

    [Test]
    public void MonitoredDeviceCreateDto_ShouldFailForInvalidIpAndRanges()
    {
        var dto = new MonitoredDeviceCreateDto
        {
            AgentId = Guid.Empty,
            DisplayName = "Switch A",
            IpAddress = "bad-ip",
            IntervalSeconds = 0,
            TimeoutMilliseconds = 0,
            FailureThreshold = 0,
            IsActive = true
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(MonitoredDeviceBaseDto.AgentId))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(MonitoredDeviceBaseDto.IpAddress))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(MonitoredDeviceBaseDto.IntervalSeconds))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(MonitoredDeviceBaseDto.TimeoutMilliseconds))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(MonitoredDeviceBaseDto.FailureThreshold))), Is.True);
    }

    [Test]
    public void MaintenanceWindowCreateDto_ShouldFailWhenEndIsNotAfterStart()
    {
        var start = DateTime.UtcNow;
        var dto = new MaintenanceWindowCreateDto
        {
            ServerRoomId = Guid.NewGuid(),
            Title = "Maintenance A",
            StartUtc = start,
            EndUtc = start,
            Description = "Planned work"
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(MaintenanceWindowBaseDto.EndUtc))), Is.True);
    }

    [Test]
    public void SensorReadingCreateDto_ShouldFailForOutOfRangeValuesAndEmptyGuid()
    {
        var dto = new SensorReadingCreateDto
        {
            ShellyDeviceId = Guid.Empty,
            TemperatureCelsius = 200,
            BatteryPercent = 120,
            Brightness = -1,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow
        };

        var results = DtoValidationHelper.Validate(dto);

        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(SensorReadingBaseDto.ShellyDeviceId))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(SensorReadingBaseDto.TemperatureCelsius))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(SensorReadingBaseDto.BatteryPercent))), Is.True);
        Assert.That(results.Any(x => x.MemberNames.Contains(nameof(SensorReadingBaseDto.Brightness))), Is.True);
    }

    [Test]
    public void ValidDtos_ShouldPassValidation()
    {
        var customer = new CustomerCreateDto
        {
            ExternalReference = "CUS-001",
            Name = "Customer A",
            ContactEmail = "customer@example.com",
            ContactPhone = "123",
            IsActive = true
        };

        var room = new ServerRoomCreateDto
        {
            CustomerId = Guid.NewGuid(),
            Name = "Room A",
            LocationDescription = "First floor",
            TemperatureWarningThreshold = 25,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        };

        var monitoredDevice = new MonitoredDeviceCreateDto
        {
            AgentId = Guid.NewGuid(),
            DisplayName = "Switch A",
            IpAddress = "10.0.0.1",
            IntervalSeconds = 30,
            TimeoutMilliseconds = 1000,
            FailureThreshold = 3,
            IsActive = true
        };

        Assert.That(DtoValidationHelper.Validate(customer), Is.Empty);
        Assert.That(DtoValidationHelper.Validate(room), Is.Empty);
        Assert.That(DtoValidationHelper.Validate(monitoredDevice), Is.Empty);
    }
}
