using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Dtos;

public record RequestStatisticsDto(
    int TotalCount,
    IReadOnlyDictionary<RequestStatus, int> ByStatus,
    IReadOnlyDictionary<RequestPriority, int> ByPriority);