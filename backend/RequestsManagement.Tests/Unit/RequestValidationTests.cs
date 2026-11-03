using System.ComponentModel.DataAnnotations;
using RequestsManagement.BL.Dtos;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.Tests.Unit;

/// <summary>
/// Input validation of the API models, run the same way ASP.NET Core runs it.
/// </summary>
public class RequestValidationTests
{
    private const string ValidRowVersion = "AAAAAAAAAAE=";

    [Fact]
    public void QueryParameters_WithDefaults_AreValid()
    {
        var errors = Validate(new RequestQueryParameters());

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void QueryParameters_WithInvalidPaging_AreRejected(int page, int pageSize)
    {
        var errors = Validate(new RequestQueryParameters { Page = page, PageSize = pageSize });

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void QueryParameters_WithReversedDateRange_AreRejected()
    {
        var parameters = new RequestQueryParameters
        {
            CreatedFrom = new DateTime(2026, 10, 1),
            CreatedTo = new DateTime(2026, 9, 1)
        };

        var errors = Validate(parameters);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RequestQueryParameters.CreatedFrom)));
    }

    [Fact]
    public void UpdateStatusRequest_WithInvalidRowVersion_IsRejected()
    {
        var update = new UpdateStatusRequest
        {
            NewStatus = RequestStatus.InProgress,
            RowVersion = "not-a-row-version"
        };

        var errors = Validate(update);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(UpdateStatusRequest.RowVersion)));
    }

    [Fact]
    public void BulkRequest_WithMoreThan100Items_IsRejected()
    {
        var bulk = new BulkUpdateStatusRequest
        {
            NewStatus = RequestStatus.InProgress,
            Items = Enumerable.Range(1, 101)
                .Select(id => new BulkUpdateItem { Id = id, RowVersion = ValidRowVersion })
                .ToList()
        };

        var errors = Validate(bulk);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(BulkUpdateStatusRequest.Items)));
    }

    [Fact]
    public void BulkRequest_WithDuplicateIds_IsRejected()
    {
        var bulk = new BulkUpdateStatusRequest
        {
            NewStatus = RequestStatus.InProgress,
            Items =
            {
                new BulkUpdateItem { Id = 7, RowVersion = ValidRowVersion },
                new BulkUpdateItem { Id = 7, RowVersion = ValidRowVersion }
            }
        };

        var errors = Validate(bulk);

        Assert.Contains(errors, e => e.ErrorMessage!.Contains("Duplicates"));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}