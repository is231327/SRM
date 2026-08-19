using System.ComponentModel.DataAnnotations;

namespace SRMUnitTests.TestHelpers;

internal static class DtoValidationHelper
{
    public static IList<ValidationResult> Validate(object dto)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true);
        return validationResults;
    }
}
