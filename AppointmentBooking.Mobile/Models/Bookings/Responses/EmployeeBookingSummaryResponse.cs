using AppointmentBooking.Mobile.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record CustomerSummaryResponse(
    long? Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone)
    {
        public string EmailDisplay =>
            string.IsNullOrWhiteSpace(Email) ? "Nije navedeno" : Email;

        public string PhoneDisplay =>
            string.IsNullOrWhiteSpace(Phone) ? "Nije navedeno" : Phone;
    }

    public sealed record EmployeeBookingSummaryResponse(
        long Id,
        CustomerSummaryResponse Customer,
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

        public string CustomerName =>
            $"{Customer.FirstName} {Customer.LastName}";

        public string TimeRange =>
            $"{LocalStartsAt:HH:mm} - {LocalEndsAt:HH:mm}";

        public string DurationText =>
            $"{TotalDurationMinutes} min";

        public string PriceText =>
            $"{TotalPrice:0.00} KM";

        public string StatusName =>
            Status switch
            {
                BookingStatus.Scheduled => "Zakazano",
                BookingStatus.Completed => "Završeno",
                BookingStatus.Cancelled => "Otkazano",
                _ => "Nepoznato"
            };
    }
}
