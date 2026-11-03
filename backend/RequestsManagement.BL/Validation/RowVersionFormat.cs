namespace RequestsManagement.BL.Validation;

/// <summary>
/// RowVersion is sent by the client as Base64 text and must decode to exactly
/// 8 bytes (the size of SQL Server's rowversion).
/// </summary>
public static class RowVersionFormat
{
    private const int Length = 8;

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var buffer = new byte[Length];
        return Convert.TryFromBase64String(value, buffer, out var bytesWritten)
            && bytesWritten == Length;
    }
}