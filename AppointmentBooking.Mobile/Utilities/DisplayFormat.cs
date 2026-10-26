using System.Globalization;

namespace AppointmentBooking.Mobile.Utilities;

/// <summary>Bosnian display formats shared by the booking details pages.</summary>
public static class DisplayFormat
{
    public static readonly CultureInfo Bosnian = CultureInfo.GetCultureInfo("bs-BA");

    /// <summary>"Petak, 23. oktobar 2026."</summary>
    public static string LongDate(DateTimeOffset date) =>
        Capitalize(date.ToString("dddd, d. MMMM yyyy.", Bosnian));

    /// <summary>"10:00 – 10:45"</summary>
    public static string TimeRange(DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        $"{startsAt.ToString("HH:mm", Bosnian)} – {endsAt.ToString("HH:mm", Bosnian)}";

    /// <summary>"45 min", "1 h", "1 h 30 min"</summary>
    public static string Duration(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} min";
        }

        int hours = minutes / 60;
        int rest = minutes % 60;
        return rest == 0 ? $"{hours} h" : $"{hours} h {rest} min";
    }

    /// <summary>"25,00 KM"</summary>
    public static string Price(decimal price) =>
        $"{price.ToString("N2", Bosnian)} KM";

    /// <summary>"1 usluga", "2 usluge", "5 usluga"</summary>
    public static string ServiceCount(int count)
    {
        int last = count % 10;
        int lastTwo = count % 100;
        bool isFew = last is >= 2 and <= 4 && lastTwo is < 12 or > 14;
        return $"{count} {(isFew ? "usluge" : "usluga")}";
    }

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToUpper(value[0], Bosnian) + value[1..];
}
