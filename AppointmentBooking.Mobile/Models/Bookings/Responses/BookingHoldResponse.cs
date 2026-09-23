using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
     public sealed record BookingHoldResponse(
        Guid HoldToken,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int TotalDurationMinutes,
        decimal TotalPrice,
        DateTimeOffset ExpiresAt);
}
