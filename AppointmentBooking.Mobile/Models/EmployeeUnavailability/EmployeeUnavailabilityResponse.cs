using System.Globalization;

namespace AppointmentBooking.Mobile.Models.EmployeeUnavailability;

public sealed record EmployeeUnavailabilityResponse(
    long Id,
    long EmployeeId,
    EmployeeUnavailabilityType Type,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAllDay,
    string? Note,
    DateTimeOffset CreatedAt)
{
    private static readonly CultureInfo BosnianCulture =
        CultureInfo.GetCultureInfo("bs-BA");

    public DateTimeOffset LocalStartsAt => StartsAt.ToLocalTime();

    public DateTimeOffset LocalEndsAt => EndsAt.ToLocalTime();

    public string TypeLabel => Type switch
    {
        EmployeeUnavailabilityType.Break => "Pauza",
        EmployeeUnavailabilityType.Vacation => "Godišnji odmor",
        _ => "Odsustvo"
    };

    public string PeriodDisplay => Type switch
    {
        EmployeeUnavailabilityType.Break =>
            $"{LocalStartsAt.ToString("dddd, d. MMMM yyyy.", BosnianCulture)} · " +
            $"{LocalStartsAt:HH:mm} – {LocalEndsAt:HH:mm}",
        EmployeeUnavailabilityType.Vacation =>
            $"{LocalStartsAt.ToString("d. MMMM yyyy.", BosnianCulture)} – " +
            $"{LocalEndsAt.AddDays(-1).ToString("d. MMMM yyyy.", BosnianCulture)}",
        _ => string.Empty
    };

    public string NoteDisplay =>
        string.IsNullOrWhiteSpace(Note)
            ? "Bez napomene"
            : Note;

    public bool CanEdit => StartsAt > DateTimeOffset.Now;

    public string StateLabel
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.Now;

            if (EndsAt <= now)
            {
                return "Prošlo";
            }

            return StartsAt <= now
                ? "U toku"
                : "Predstojeće";
        }
    }
}
