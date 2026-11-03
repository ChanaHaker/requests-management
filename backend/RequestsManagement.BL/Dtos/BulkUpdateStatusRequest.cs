using System.ComponentModel.DataAnnotations;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Dtos;

/// <summary>
/// Changes the status of up to 100 requests in one call.
/// Each item is processed independently (partial success).
/// </summary>
public class BulkUpdateStatusRequest : IValidatableObject
{
    public const int MaxItems = 100;

    [Required]
    public RequestStatus? NewStatus { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one request is required.")]
    [MaxLength(MaxItems, ErrorMessage = "A bulk update can include at most 100 requests.")]
    public List<BulkUpdateItem> Items { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NewStatus.HasValue && !Enum.IsDefined(NewStatus.Value))
        {
            yield return new ValidationResult("NewStatus is not a valid value.", new[] { nameof(NewStatus) });
        }

        var duplicates = Items
            .GroupBy(i => i.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            yield return new ValidationResult(
                $"Each request may appear only once. Duplicates: {string.Join(", ", duplicates)}.",
                new[] { nameof(Items) });
        }
    }
}