using AppointmentBooking.Mobile.Models.Bookings.Responses;

namespace AppointmentBooking.Mobile.Services.BookingCalendar;

public interface IBookingCalendarService
{
    Task<BookingCalendarDaysResponse> GetWorkingDaysAsync(long employeeId);

    void Invalidate(long employeeId);
}
