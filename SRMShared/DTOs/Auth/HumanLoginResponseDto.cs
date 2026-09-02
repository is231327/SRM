namespace SRMShared.DTOs.Auth;

public class HumanLoginResponseDto
{
    public string ChallengeToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool RequiresEnrollment { get; set; }
    public TotpSetupDto? Setup { get; set; }
}

public class TotpSetupDto
{
    public string ManualEntryKey { get; set; } = string.Empty;
    public string QrCodeSvgDataUrl { get; set; } = string.Empty;
}
