namespace SRMAuth.Configuration;

public class LoginSecurityOptions
{
    public const string SectionName = "LoginSecurity";
    public int MaximumFailures { get; set; } = 5;
    public int FailureWindowMinutes { get; set; } = 15;
}
