using System.ComponentModel.DataAnnotations;
using System.Net;

namespace SRMShared.Attributes;

public sealed class IpAddressAttribute : ValidationAttribute
{
    public IpAddressAttribute()
    {
        ErrorMessage = "The field must contain a valid IP address.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is string ipAddress
            && !string.IsNullOrWhiteSpace(ipAddress)
            && IPAddress.TryParse(ipAddress, out _);
    }
}
