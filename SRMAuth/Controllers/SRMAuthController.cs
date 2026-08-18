using Microsoft.AspNetCore.Mvc;

namespace SRMAuth.Controllers;

[ApiController]
[Route("[controller]")]
public class SRMAuthController : ControllerBase
{
    private static List<AuthData> demoData = new List<AuthData>()
    {  
        new AuthData() { AccessLevel = 9, Token = "abc", TokenExpire = DateTime.Now.AddMinutes(30), Username = "admin" },
        new AuthData() { AccessLevel = 0, Token = "xyz", TokenExpire = DateTime.Now.AddMinutes(30), Username = "user" }
    };
    public SRMAuthController()
    {
    }

    [HttpGet]
    public List<AuthData> Get(string Token)
    {
        //HACK verify Token
        return demoData;
    }

    [HttpGet]
    [Route("GetData")]
    public AuthData Get(string Username, string Password)
    {
        AuthData? result = demoData.Find(d => d.Username == Username);
        //HACK check password and create Token
        return result ?? new AuthData();
    }

    [HttpPost]
    public IActionResult POST(string Name, string Password, int AccessLevel)
    {
        try
        {
            AuthData result = new AuthData() { Username = Name, AccessLevel = AccessLevel };
            demoData.Add(result);
            return StatusCode(200);
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }
}
