namespace AppointmentBooking.Mobile.Utilities;

/// <summary>Salon business dates and times always use Bosnia and Herzegovina time.</summary>
public static class SalonTimeZone
{
    public static TimeZoneInfo Info { get; } = Resolve();

    public static DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Info).DateTime);

    public static DateTimeOffset ToLocal(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Info);

    public static DateTimeOffset At(DateTime localDateTime)
    {
        DateTime unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, Info.GetUtcOffset(unspecified));
    }

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
    }
}
