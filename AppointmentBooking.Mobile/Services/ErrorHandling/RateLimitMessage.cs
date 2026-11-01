namespace AppointmentBooking.Mobile.Services.ErrorHandling;

/// <summary>
/// The message shown when the API answers 429 Too Many Requests (rate limit),
/// with the waiting time from the Retry-After header.
/// </summary>
public static class RateLimitMessage
{
    public const string Title = "Previše pokušaja";

    public static string Create(TimeSpan? retryAfter) =>
        $"Napravili ste previše pokušaja u kratkom vremenu. Pokušajte ponovo {FormatWait(retryAfter)}.";

    private static string FormatWait(TimeSpan? retryAfter)
    {
        if (retryAfter is not TimeSpan wait || wait <= TimeSpan.Zero)
        {
            return "za nekoliko minuta";
        }

        if (wait < TimeSpan.FromMinutes(1))
        {
            int seconds = (int)Math.Ceiling(wait.TotalSeconds);
            return $"za {seconds} {Plural(seconds, "sekundu", "sekunde", "sekundi")}";
        }

        if (wait <= TimeSpan.FromMinutes(60))
        {
            int minutes = (int)Math.Ceiling(wait.TotalMinutes);
            return $"za {minutes} {Plural(minutes, "minutu", "minute", "minuta")}";
        }

        int hours = (int)Math.Ceiling(wait.TotalHours);
        return $"za {hours} {Plural(hours, "sat", "sata", "sati")}";
    }

    // Bosnian plural: 1, 21, 31… → one; 2–4, 22–24… → few; otherwise many (11–14 included).
    private static string Plural(int value, string one, string few, string many)
    {
        int lastTwo = value % 100;
        int last = value % 10;

        if (last == 1 && lastTwo != 11)
        {
            return one;
        }

        if (last is >= 2 and <= 4 && lastTwo is < 12 or > 14)
        {
            return few;
        }

        return many;
    }
}
