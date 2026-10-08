namespace AppointmentBooking.Mobile.Models.Bookings.Requests;

public sealed record CreateStaffBookingRequest(
    string CustomerFirstName,
    string CustomerLastName,
    long? EmployeeId,
    DateTimeOffset StartsAt,
    IReadOnlyCollection<long> ServiceIds,
    string? Notes);
