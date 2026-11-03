using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RequestsManagement.BL.Dtos;
using RequestsManagement.BL.Services;

namespace RequestsManagement.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public class RequestsController : ControllerBase
{
    // Authentication is out of scope, so the current user name is sent in a header.
    private const string UserNameHeader = "X-User-Name";

    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// Returns one page of requests. Filtering, search, sorting and paging run on the server.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RequestDto>>> GetRequests(
        [FromQuery] RequestQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _requestService.GetRequestsAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single request by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var request = await _requestService.GetByIdAsync(id, cancellationToken);
        return Ok(request);
    }

    /// <summary>
    /// Returns summary counts by status and by priority.
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(RequestStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestStatisticsDto>> GetStatistics(CancellationToken cancellationToken)
    {
        var statistics = await _requestService.GetStatisticsAsync(cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// Changes the status of a request. The body must include the RowVersion the client last read;
    /// if the request was changed by someone else since then, 409 Conflict is returned.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestDto>> UpdateStatus(
        int id,
        [FromBody] UpdateStatusRequest update,
        [FromHeader(Name = UserNameHeader), Required, StringLength(100)] string changedBy,
        CancellationToken cancellationToken)
    {
        var updated = await _requestService.UpdateStatusAsync(id, update, changedBy, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Returns the status change history of a request, newest first.
    /// </summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<RequestStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RequestStatusHistoryDto>>> GetHistory(
        int id,
        CancellationToken cancellationToken)
    {
        var history = await _requestService.GetHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}