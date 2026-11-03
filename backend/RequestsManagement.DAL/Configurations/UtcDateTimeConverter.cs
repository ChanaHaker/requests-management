using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RequestsManagement.DAL.Configurations;

/// <summary>
/// All dates are stored in UTC. SQL Server's datetime2 does not keep the "kind",
/// so values read back are explicitly marked as UTC (serialized with a trailing "Z").
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            toDatabase => toDatabase.Kind == DateTimeKind.Local
                ? toDatabase.ToUniversalTime()
                : DateTime.SpecifyKind(toDatabase, DateTimeKind.Utc),
            fromDatabase => DateTime.SpecifyKind(fromDatabase, DateTimeKind.Utc))
    {
    }
}