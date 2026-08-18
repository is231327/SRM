using Microsoft.AspNetCore.Mvc;

namespace SRMAgent.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentDataController : ControllerBase
{
    private static AgentData demoData = new AgentData()
    {
        AgentId = Guid.NewGuid().ToString(),
        CustomerId = Guid.NewGuid().ToString(),
        ShellyId = Guid.NewGuid().ToString(),
        CurrentBattery = 90,
        CurrentTemp = 24,
        DoorOpen = false,
        KeepAliveTimestamp = DateTime.Now
    };

    public AgentDataController()
    {
    }

    [HttpGet]
    public AgentData Get()
    {
        demoData.CurrentTemp = new Random().Next(10, 30);
        demoData.KeepAliveTimestamp = DateTime.Now;
        return demoData;
    }
}
