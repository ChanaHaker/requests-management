using RequestsManagement.BL.Validation;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.Tests.Unit;

/// <summary>
/// Business rules for status changes (no database or API involved).
/// </summary>
public class StatusTransitionsTests
{
    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Waiting)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress)]
    public void IsAllowed_ReturnsTrue_ForAllowedTransitions(RequestStatus from, RequestStatus to)
    {
        var allowed = StatusTransitions.IsAllowed(from, to);

        Assert.True(allowed);
    }

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.Waiting)]
    [InlineData(RequestStatus.New, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Completed)]
    [InlineData(RequestStatus.Completed, RequestStatus.New)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.InProgress)]
    public void IsAllowed_ReturnsFalse_ForForbiddenTransitions(RequestStatus from, RequestStatus to)
    {
        var allowed = StatusTransitions.IsAllowed(from, to);

        Assert.False(allowed);
    }

    [Fact]
    public void Completed_IsFinalStatus_WithNoAllowedTargets()
    {
        var targets = StatusTransitions.GetAllowedTargets(RequestStatus.Completed);

        Assert.Empty(targets);
    }
}