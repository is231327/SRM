using System.ComponentModel.DataAnnotations;

namespace SRMShared.Attributes;

public sealed class NonEmptyGuidAttribute : ValidationAttribute
{
    public NonEmptyGuidAttribute()
    {
        ErrorMessage = "The field must contain a non-empty GUID.";
    }

    public override bool IsValid(object? value)
    {
        return value is Guid guid && guid != Guid.Empty;
    }
}
