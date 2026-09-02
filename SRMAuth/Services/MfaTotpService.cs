using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using QRCoder;
using SRMAuth.Configuration;
using SRMAuth.Services.Interfaces;

namespace SRMAuth.Services;

public class MfaTotpService(IDataProtectionProvider dataProtectionProvider, IOptions<MfaOptions> options, TimeProvider timeProvider) : IMfaTotpService
{
    private const int TimeStepSeconds = 30;
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("SRMAuth.MfaSecret.v1");
    private readonly MfaOptions options = options.Value;

    public string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(20));

    public string ProtectSecret(string secret) => protector.Protect(secret);

    public string UnprotectSecret(string protectedSecret) => protector.Unprotect(protectedSecret);

    public string BuildQrCodeSvgDataUrl(string username, string secret)
    {
        var issuer = string.IsNullOrWhiteSpace(options.Issuer) ? "SRM" : options.Issuer.Trim();
        var label = Uri.EscapeDataString($"{issuer}:{username}");
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period={TimeStepSeconds}";
        using var data = QRCodeGenerator.GenerateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(4);
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
    }

    public bool TryVerifyTotp(string secret, string code, long? lastUsedTimeStep, out long matchedTimeStep)
    {
        matchedTimeStep = 0;
        var normalizedCode = new string(code.Where(x => !char.IsWhiteSpace(x)).ToArray());
        if (normalizedCode.Length != 6 || normalizedCode.Any(x => !char.IsDigit(x)))
        {
            return false;
        }

        var key = Base32Decode(secret);
        var currentStep = timeProvider.GetUtcNow().ToUnixTimeSeconds() / TimeStepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            var candidateStep = currentStep + offset;
            if (lastUsedTimeStep.HasValue && candidateStep <= lastUsedTimeStep.Value)
            {
                continue;
            }

            var candidate = ComputeTotp(key, candidateStep);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(candidate),
                    Encoding.ASCII.GetBytes(normalizedCode)))
            {
                matchedTimeStep = candidateStep;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 10)
        => Enumerable.Range(0, count)
            .Select(_ =>
            {
                var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
                return $"{value[..4]}-{value[4..8]}-{value[8..12]}-{value[12..16]}";
            })
            .ToArray();

    public string HashRecoveryCode(string code)
    {
        var normalized = NormalizeRecoveryCode(code);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string NormalizeRecoveryCode(string code)
        => new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string ComputeTotp(byte[] key, long timeStep)
    {
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, timeStep);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];
        return (binaryCode % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0)
        {
            output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }
        return output.ToString();
    }

    private static byte[] Base32Decode(string value)
    {
        var normalized = value.Trim().TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var character in normalized)
        {
            var index = character is >= 'A' and <= 'Z' ? character - 'A'
                : character is >= '2' and <= '7' ? character - '2' + 26
                : throw new FormatException("The MFA secret is invalid.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }
        return output.ToArray();
    }
}
