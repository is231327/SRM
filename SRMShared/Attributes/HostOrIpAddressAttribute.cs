using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;

namespace SRMShared.Attributes;

public sealed partial class HostOrIpAddressAttribute : ValidationAttribute
{
    public HostOrIpAddressAttribute()
    {
        ErrorMessage = "The field must contain a valid host name or IP address.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not string input || string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return IPAddress.TryParse(input, out _) || HostNameRegex().IsMatch(input);
    }

    [GeneratedRegex(@"^(?=.{1,253}$)(?!-)(?:[A-Za-z0-9-]{1,63}(?<!-))(?:\.(?!-)(?:[A-Za-z0-9-]{1,63}(?<!-)))*$", RegexOptions.Compiled)]
    private static partial Regex HostNameRegex();
}
