namespace CampusEstateLiving.Services;

public static class SaTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"); }
            catch { return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST"); }
        }
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateTime ToLocal(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc.ToUniversalTime(), Zone);
    }

    public static string Format(DateTime utc) => ToLocal(utc).ToString("dd MMM yyyy, HH:mm");

    public static string FormatDate(DateTime utc) => ToLocal(utc).ToString("dd MMM yyyy");

    public static string Greeting()
    {
        var hour = Now.Hour;
        if (hour < 12) return "Good morning";
        if (hour < 18) return "Good afternoon";
        return "Good evening";
    }
}
