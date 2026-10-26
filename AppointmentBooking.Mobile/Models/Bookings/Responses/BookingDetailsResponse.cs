using AppointmentBooking.Mobile.Models.Appointments.Responses;
using AppointmentBooking.Mobile.Models.Employees.Responses;
using AppointmentBooking.Mobile.Models.Enums;
using AppointmentBooking.Mobile.Utilities;
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
            SalonTimeZone.ToLocal(StartsAt);

        public DateTimeOffset LocalEndsAt =>
            SalonTimeZone.ToLocal(EndsAt);

        public DateTimeOffset? LocalCompletedAt =>
            CompletedAt.HasValue ? SalonTimeZone.ToLocal(CompletedAt.Value) : null;

        public string EmployeeName =>
            $"{Employee.FirstName} {Employee.LastName}";

        public string StartsAtDateText =>
            DisplayFormat.LongDate(LocalStartsAt);

        public string TimeRangeText =>
            DisplayFormat.TimeRange(LocalStartsAt, LocalEndsAt);

        public string TotalDurationText =>
            DisplayFormat.Duration(TotalDurationMinutes);

        public string TotalPriceText =>
            DisplayFormat.Price(TotalPrice);

        public string ServiceCountText =>
            DisplayFormat.ServiceCount(Appointments.Count);

        public bool HasNotes =>
            !string.IsNullOrWhiteSpace(Notes);

        public bool IsCancelled =>
            Status == BookingStatus.Cancelled;

        public string StatusName => Status switch
        {
            BookingStatus.Scheduled => "Zakazano",
            BookingStatus.Completed => "Završeno",
            BookingStatus.Cancelled => "Otkazano",
            _ => "Nepoznato"
        };
    }
}
