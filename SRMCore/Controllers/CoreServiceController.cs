using Microsoft.AspNetCore.Mvc;

namespace SRMCore.Controllers;

[ApiController]
[Route("[controller]")]
public class CoreServiceController : ControllerBase
{
    private static List<CoreServiceData> demoData = new List<CoreServiceData>()
    {
        new CoreServiceData(){ AgentId = Guid.NewGuid().ToString(), CustomerId = Guid.NewGuid().ToString(), CustomerName = "Peter Griffin" },
        new CoreServiceData(){ AgentId = Guid.NewGuid().ToString(), CustomerId = Guid.NewGuid().ToString(), CustomerName = "Wonderwoman" },
        new CoreServiceData(){ AgentId = Guid.NewGuid().ToString(), CustomerId = Guid.NewGuid().ToString(), CustomerName = "Snoopy" }
    };

    public CoreServiceController()
    {
    }

    [HttpGet]
    public List<CoreServiceData> Get()
    {
        return demoData;
    }

    [HttpGet]
    [Route("GetCustomer")]
    public CoreServiceData Get(string CustomerID)
    {
        CoreServiceData? result = demoData.Find(d => d.CustomerId == CustomerID);
        return result ?? new CoreServiceData();
    }

    [HttpPost]
    public IActionResult POST(string Name, string CustomerId)
    {
        try
        {
            CoreServiceData? result = demoData.Find(d => d.CustomerId == CustomerId);
            if (result != null)
            {
                result.CustomerName = Name;
            }
            else
            {
                return StatusCode(500);
            }

            return StatusCode(200);
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

    [HttpPut]
    public IActionResult PUT(string Name)
    {
        try
        {
            CoreServiceData result = new CoreServiceData() { AgentId = Guid.NewGuid().ToString(), CustomerId = Guid.NewGuid().ToString(), CustomerName = Name };
            demoData.Add(result);
            return StatusCode(200);
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

}
