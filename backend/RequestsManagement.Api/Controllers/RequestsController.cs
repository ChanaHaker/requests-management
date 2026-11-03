using Microsoft.AspNetCore.Mvc;
using RequestsManagement.BL.Dtos;
using RequestsManagement.BL.Services;

namespace RequestsManagement.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
}