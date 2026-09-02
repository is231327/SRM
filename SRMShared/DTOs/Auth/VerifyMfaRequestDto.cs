using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class VerifyMfaRequestDto
{
    [Required]
    [StringLength(200)]
    public string ChallengeToken { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;
}

public class MfaAuthenticationResponseDto
{
    public AuthTokenResponseDto Token { get; set; } = new();
    public IReadOnlyCollection<string> RecoveryCodes { get; set; } = Array.Empty<string>();
}
