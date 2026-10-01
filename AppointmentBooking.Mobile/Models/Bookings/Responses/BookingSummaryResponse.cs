using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record BookingSummaryResponse(
        long Id,
        BookableEmployeeResponse Employee,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int TotalDurationMinutes,
        decimal TotalPrice,
        BookingStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt)
    {
       public DateTimeOffset LocalStartsAt =>
            StartsAt.ToLocalTime();

        public DateTimeOffset LocalEndsAt =>
            EndsAt.ToLocalTime();
    }
}
