using System.ComponentModel.DataAnnotations;
using RequestsManagement.BL.Validation;

namespace RequestsManagement.BL.Dtos;

/// <summary>
/// One request in a bulk status update, with the version the client last read.
/// </summary>
public class BulkUpdateItem : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive number.")]
    public int Id { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(RowVersion) && !RowVersionFormat.IsValid(RowVersion))
        {
            yield return new ValidationResult("RowVersion is not in a valid format.", new[] { nameof(RowVersion) });
        }
    }
}