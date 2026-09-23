using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record AvailableTimeSlotResponse(
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);
}
