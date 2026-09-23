using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record BookingAvailabilityResponse(
        long EmployeeId,
        DateOnly Date,
        int TotalDurationMinutes,
        IReadOnlyList<AvailableTimeSlotResponse> Slots);
}
