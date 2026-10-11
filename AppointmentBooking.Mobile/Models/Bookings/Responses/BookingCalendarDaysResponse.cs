namespace AppointmentBooking.Mobile.Models.Bookings.Responses;

public sealed record BookingCalendarDaysResponse(
    long EmployeeId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DateOnly> WorkingDates);
