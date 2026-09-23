using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Requests
{
    public sealed class CreateBookingRequest
    {
        public long EmployeeId { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public IReadOnlyCollection<long> ServiceIds { get; init; } = [];

        public string? Notes { get; init; }
    }
}
