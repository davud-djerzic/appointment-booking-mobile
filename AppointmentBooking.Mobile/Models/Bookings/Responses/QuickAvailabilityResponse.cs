using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record QuickAvailabilityResponse(
        long EmployeeId,
        long ServiceId,
        string ServiceName,
        int DurationMinutes,
        decimal Price,
        DateOnly? Date,
        IReadOnlyList<AvailableTimeSlotResponse> AvailableSlots);
}
