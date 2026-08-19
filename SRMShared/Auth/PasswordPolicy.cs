namespace SRMShared.Auth;

public static class PasswordPolicy
{
    public const int MinLength = 12;

    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must contain at least one special character.");
        }

        return errors;
    }

    public static string GetSummary()
    {
        return $"At least {MinLength} characters, with uppercase, lowercase, digit, and special character.";
    }
}
