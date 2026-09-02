namespace SRMAuth.Configuration;

public class MfaOptions
{
    public const string SectionName = "Mfa";

    public string Issuer { get; set; } = "SRM";
    public int ChallengeLifetimeMinutes { get; set; } = 5;
}
