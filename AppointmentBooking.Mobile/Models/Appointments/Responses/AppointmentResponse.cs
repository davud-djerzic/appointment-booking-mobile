using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Appointments.Responses
{
    public sealed record AppointmentResponse(
        long Id,
        long BookingId,
        long ServiceId,
        string ServiceName,
        int DurationMinutes,
        decimal Price,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);
}
