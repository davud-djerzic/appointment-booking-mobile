namespace AppointmentBooking.Mobile.Models.EmployeeUnavailability;

public sealed record SetEmployeeUnavailabilityRequest(
    EmployeeUnavailabilityType Type,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAllDay,
    string? Note);
