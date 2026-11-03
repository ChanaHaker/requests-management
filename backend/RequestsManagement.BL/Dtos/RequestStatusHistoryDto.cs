using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Dtos;

public record RequestStatusHistoryDto(
    long Id,
    int RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy);