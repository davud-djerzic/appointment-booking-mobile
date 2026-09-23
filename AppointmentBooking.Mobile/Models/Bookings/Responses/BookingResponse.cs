using AppointmentBooking.Mobile.Models.Appointments.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record BookingResponse(
        long Id,
        long EmployeeId,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        IReadOnlyList<AppointmentResponse> Appointments);
}
