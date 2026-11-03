using RequestsManagement.DAL.Entities;

namespace RequestsManagement.BL.Validation;

/// <summary>
/// Business rules for status changes. Completed is a final status.
/// New -> InProgress
/// InProgress -> Waiting | Completed
/// Waiting -> InProgress
/// </summary>
public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = new[] { RequestStatus.InProgress },
            [RequestStatus.InProgress] = new[] { RequestStatus.Waiting, RequestStatus.Completed },
            [RequestStatus.Waiting] = new[] { RequestStatus.InProgress },
            [RequestStatus.Completed] = Array.Empty<RequestStatus>()
        };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyCollection<RequestStatus> GetAllowedTargets(RequestStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<RequestStatus>();
}