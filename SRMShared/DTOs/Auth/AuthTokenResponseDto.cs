namespace SRMShared.DTOs.Auth;

public class AuthTokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Username { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public Guid? CustomerId { get; set; }
    public Guid? AgentId { get; set; }
}
