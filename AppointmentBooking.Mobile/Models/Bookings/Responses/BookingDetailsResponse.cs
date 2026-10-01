using AppointmentBooking.Mobile.Models.Appointments.Responses;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record BookingDetailsResponse(
        long Id,
        BookableEmployeeResponse Employee,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int TotalDurationMinutes,
        decimal TotalPrice,
        BookingStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        string? CompletionSource,
        IReadOnlyList<AppointmentResponse> Appointments)
    {
        public DateTimeOffset LocalStartsAt =>
            StartsAt.ToLocalTime();

        public DateTimeOffset LocalEndsAt =>
            EndsAt.ToLocalTime();

        public DateTimeOffset? LocalCompletedAt =>
            CompletedAt?.ToLocalTime();

        public string EmployeeName =>
            $"{Employee.FirstName} {Employee.LastName}";

        public string StatusName => Status switch
        {
            BookingStatus.Scheduled => "Zakazano",
            BookingStatus.Completed => "Završeno",
            BookingStatus.Cancelled => "Otkazano",
            _ => "Nepoznato"
        };
    }
}
