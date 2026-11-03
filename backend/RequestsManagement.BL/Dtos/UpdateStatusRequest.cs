using System.ComponentModel.DataAnnotations;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Dtos;

/// <summary>
/// Body of a status update. RowVersion is the version the client last read;
/// the update succeeds only if the request has not changed since then.
/// </summary>
public class UpdateStatusRequest : IValidatableObject
{
    private const int RowVersionLength = 8;

    [Required]
    public RequestStatus? NewStatus { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NewStatus.HasValue && !Enum.IsDefined(NewStatus.Value))
        {
            yield return new ValidationResult("NewStatus is not a valid value.", new[] { nameof(NewStatus) });
        }

        if (!string.IsNullOrWhiteSpace(RowVersion) && !IsValidRowVersion(RowVersion))
        {
            yield return new ValidationResult("RowVersion is not in a valid format.", new[] { nameof(RowVersion) });
        }
    }

    private static bool IsValidRowVersion(string value)
    {
        var buffer = new byte[RowVersionLength];
        return Convert.TryFromBase64String(value, buffer, out var bytesWritten)
            && bytesWritten == RowVersionLength;
    }
}