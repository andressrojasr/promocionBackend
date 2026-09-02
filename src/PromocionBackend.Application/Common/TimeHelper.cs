namespace PromocionBackend.Application.Common;

public static class TimeHelper
{
    // Zona horaria de Ecuador (UTC-5, sin observancia de horario de verano)
    private static readonly TimeZoneInfo EcuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    public static DateTime GetEcuadorNow()
    {
        var utcNow = DateTime.UtcNow;
        return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.Utc, EcuadorTimeZone);
    }

    public static DateTime ConvertToEcuadorTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Utc, EcuadorTimeZone);
    }

    public static string FormatToEcuadorString(DateTime utcDateTime)
    {
        var ecuadorTime = TimeZoneInfo.ConvertTime(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            EcuadorTimeZone);
        var offset = EcuadorTimeZone.GetUtcOffset(ecuadorTime);
        var dateTimeOffset = new DateTimeOffset(ecuadorTime, offset);
        return dateTimeOffset.ToString("O");
    }
}
