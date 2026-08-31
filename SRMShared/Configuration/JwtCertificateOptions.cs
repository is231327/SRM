namespace SRMShared.Configuration;

/// <summary>
/// Configuration model for a JWT certificate.
/// SRMAuth uses this for signing (private key). SRMCore uses it for validation (public key).
/// </summary>
public class JwtCertificateOptions
{
    /// <summary>
    /// The thumbprint of the certificate.
    /// Used when looking up a certificate in the Windows certificate store.
    /// </summary>
    public string? Thumbprint { get; set; }

    /// <summary>
    /// The certificate store to search (e.g. "CurrentUser" or "LocalMachine").
    /// </summary>
    public string? Store { get; set; }

    /// <summary>
    /// The certificate store location (e.g. "My" for Personal, "Root" for Trusted Root).
    /// </summary>
    public string? StoreLocation { get; set; }

    /// <summary>
    /// Full path to the certificate file (.pfx for private key, .cer for public key).
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// The password for the certificate file.
    /// Only used when Path is set and the file is a .pfx.
    /// </summary>
    public string? Password { get; set; }
}
