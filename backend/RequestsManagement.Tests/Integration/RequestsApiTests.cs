using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RequestsManagement.BL.Dtos;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.Tests.Integration;

/// <summary>
/// End-to-end tests: HTTP request -> controller -> service -> repository -> SQL Server and back.
/// Each test creates its own data under a unique organization name, so tests do not affect each other.
/// </summary>
public class RequestsApiTests : IClassFixture<ApiFactory>
{
    private const string UnknownRowVersion = "AAAAAAAAAAE=";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public RequestsApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task GetRequests_WithFilterAndPaging_ReturnsOnlyMatchingPage()
    {
        // Arrange: 2 matching requests, plus 2 that must be filtered out.
        var organization = UniqueOrganization();
        await _factory.AddRequestAsync(organization, priority: RequestPriority.High);
        await _factory.AddRequestAsync(organization, priority: RequestPriority.High);
        await _factory.AddRequestAsync(organization, priority: RequestPriority.Low);
        await _factory.AddRequestAsync(UniqueOrganization(), priority: RequestPriority.High);

        // Act: page 1 with a page size of 1.
        var response = await _client.GetAsync(
            $"/api/requests?organizationName={Uri.EscapeDataString(organization)}&priority=High&page=1&pageSize=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<RequestDto>>(ApiFactory.Json);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);

        var item = Assert.Single(page.Items);
        Assert.Equal(organization, item.OrganizationName);
        Assert.Equal(RequestPriority.High, item.Priority);
    }

    [Fact]
    public async Task UpdateStatus_WithStaleRowVersion_ReturnsConflictAndKeepsFirstUpdate()
    {
        // Arrange: both users read the same version.
        var request = await _factory.AddRequestAsync(UniqueOrganization());
        var versionBothUsersSaw = Convert.ToBase64String(request.RowVersion);

        // Act
        var rachel = await PatchStatusAsync(request.Id, RequestStatus.InProgress, versionBothUsersSaw, "rachel");
        var sarah = await PatchStatusAsync(request.Id, RequestStatus.InProgress, versionBothUsersSaw, "sarah");

        // Assert: the first update wins, the second gets 409.
        Assert.Equal(HttpStatusCode.OK, rachel.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, sarah.StatusCode);

        var problem = await sarah.Content.ReadFromJsonAsync<ProblemDetails>(ApiFactory.Json);
        Assert.Equal(409, problem!.Status);

        // Only the successful update was audited.
        var history = await _client.GetFromJsonAsync<List<RequestStatusHistoryDto>>(
            $"/api/requests/{request.Id}/history", ApiFactory.Json);

        var entry = Assert.Single(history!);
        Assert.Equal("rachel", entry.ChangedBy);
        Assert.Equal(RequestStatus.New, entry.PreviousStatus);
        Assert.Equal(RequestStatus.InProgress, entry.NewStatus);
    }

    [Fact]
    public async Task UpdateStatus_TwoSimultaneousUpdates_OnlyOneSucceeds()
    {
        // Arrange
        var request = await _factory.AddRequestAsync(UniqueOrganization());
        var version = Convert.ToBase64String(request.RowVersion);

        // Act: both requests are sent at the same time.
        // Both may pass the early version check; the database decides (WHERE RowVersion = ...).
        var responses = await Task.WhenAll(
            PatchStatusAsync(request.Id, RequestStatus.InProgress, version, "rachel"),
            PatchStatusAsync(request.Id, RequestStatus.InProgress, version, "sarah"));

        // Assert: exactly one success and one conflict - no lost update.
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateStatus_ForMissingRequest_ReturnsNotFound()
    {
        var response = await PatchStatusAsync(int.MaxValue, RequestStatus.InProgress, UnknownRowVersion, "rachel");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_WithMixedItems_ReturnsPartialSuccess()
    {
        // Arrange
        var organization = UniqueOrganization();
        var valid = await _factory.AddRequestAsync(organization);
        var stale = await _factory.AddRequestAsync(organization);
        var completed = await _factory.AddRequestAsync(organization, RequestStatus.Completed);

        var body = new BulkUpdateStatusRequest
        {
            NewStatus = RequestStatus.InProgress,
            Items =
            {
                new BulkUpdateItem { Id = valid.Id, RowVersion = Convert.ToBase64String(valid.RowVersion) },
                new BulkUpdateItem { Id = stale.Id, RowVersion = UnknownRowVersion },
                new BulkUpdateItem { Id = completed.Id, RowVersion = Convert.ToBase64String(completed.RowVersion) },
                new BulkUpdateItem { Id = int.MaxValue, RowVersion = UnknownRowVersion }
            }
        };

        // Act
        using var message = new HttpRequestMessage(HttpMethod.Patch, "/api/requests/status")
        {
            Content = JsonContent.Create(body, options: ApiFactory.Json)
        };
        message.Headers.Add("X-User-Name", "david");

        var response = await _client.SendAsync(message);

        // Assert: the call succeeds and every item has its own outcome.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkUpdateStatusResult>(ApiFactory.Json);
        Assert.NotNull(result);
        Assert.Equal(4, result.Requested);
        Assert.Equal(1, result.Updated);
        Assert.Equal(3, result.Failed);

        Assert.Equal(StatusChangeOutcome.Updated, OutcomeOf(result, valid.Id));
        Assert.Equal(StatusChangeOutcome.Conflict, OutcomeOf(result, stale.Id));
        Assert.Equal(StatusChangeOutcome.InvalidTransition, OutcomeOf(result, completed.Id));
        Assert.Equal(StatusChangeOutcome.NotFound, OutcomeOf(result, int.MaxValue));
    }

    // ---------- Helpers ----------

    private async Task<HttpResponseMessage> PatchStatusAsync(
        int id,
        RequestStatus newStatus,
        string rowVersion,
        string userName)
    {
        using var message = new HttpRequestMessage(HttpMethod.Patch, $"/api/requests/{id}/status")
        {
            Content = JsonContent.Create(
                new UpdateStatusRequest { NewStatus = newStatus, RowVersion = rowVersion },
                options: ApiFactory.Json)
        };
        message.Headers.Add("X-User-Name", userName);

        return await _client.SendAsync(message);
    }

    private static StatusChangeOutcome OutcomeOf(BulkUpdateStatusResult result, int id) =>
        result.Results.Single(r => r.Id == id).Outcome;

    private static string UniqueOrganization() => $"Org {Guid.NewGuid():N}";
}