namespace CampusEstateLiving.Api.Services;

public static class SaClock
{
    public static DateTime Now()
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow.AddHours(2);
        }
        catch (InvalidTimeZoneException)
        {
            return DateTime.UtcNow.AddHours(2);
        }
    }

    public static string Greeting()
    {
        var hour = Now().Hour;
        if (hour < 12) return "Good morning";
        if (hour < 17) return "Good afternoon";
        return "Good evening";
    }

    public static DateTime MonthStartUtc()
    {
        var now = Now();
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
