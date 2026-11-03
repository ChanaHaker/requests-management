using System.ComponentModel.DataAnnotations;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DAL.Repositories;

namespace RequestsManagement.BL.Dtos;

/// <summary>
/// Paging, filtering, search and sorting options for the requests list.
/// Validation runs automatically before the controller action is called.
/// </summary>
public class RequestQueryParameters : IValidatableObject
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    public RequestStatus? Status { get; set; }

    public RequestPriority? Priority { get; set; }

    [MaxLength(200)]
    public string? OrganizationName { get; set; }

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    [MaxLength(100)]
    public string? Search { get; set; }

    public RequestSortField SortBy { get; set; } = RequestSortField.CreatedAt;

    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Enums also accept undefined numbers (e.g. status=9), so they are checked explicitly.
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
        {
            yield return new ValidationResult("Status is not a valid value.", new[] { nameof(Status) });
        }

        if (Priority.HasValue && !Enum.IsDefined(Priority.Value))
        {
            yield return new ValidationResult("Priority is not a valid value.", new[] { nameof(Priority) });
        }

        if (!Enum.IsDefined(SortBy))
        {
            yield return new ValidationResult("SortBy is not a valid field.", new[] { nameof(SortBy) });
        }

        if (!Enum.IsDefined(SortDirection))
        {
            yield return new ValidationResult("SortDirection must be Asc or Desc.", new[] { nameof(SortDirection) });
        }

        if (CreatedFrom.HasValue && CreatedTo.HasValue && CreatedFrom > CreatedTo)
        {
            yield return new ValidationResult(
                "CreatedFrom must be earlier than or equal to CreatedTo.",
                new[] { nameof(CreatedFrom), nameof(CreatedTo) });
        }
    }
}