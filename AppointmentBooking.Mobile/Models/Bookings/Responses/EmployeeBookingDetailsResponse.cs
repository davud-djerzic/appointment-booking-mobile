using System.Globalization;
using AppointmentBooking.Mobile.Models.Appointments.Responses;
using AppointmentBooking.Mobile.Models.Enums;
using AppointmentBooking.Mobile.Utilities;

namespace AppointmentBooking.Mobile.Models.Bookings.Responses
{
    public sealed record EmployeeBookingDetailsResponse(
        long Id,
        CustomerSummaryResponse Customer,
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
        private static readonly CultureInfo BosnianCulture =
            CultureInfo.GetCultureInfo("bs-BA");

        public DateTimeOffset LocalStartsAt =>
            SalonTimeZone.ToLocal(StartsAt);

        public DateTimeOffset LocalEndsAt =>
            SalonTimeZone.ToLocal(EndsAt);

        public DateTimeOffset LocalCreatedAt =>
            SalonTimeZone.ToLocal(CreatedAt);

        public DateTimeOffset? LocalCompletedAt =>
            CompletedAt.HasValue ? SalonTimeZone.ToLocal(CompletedAt.Value) : null;

        public bool HasCompletedAt =>
            CompletedAt.HasValue;

        public string CustomerName =>
            $"{Customer.FirstName} {Customer.LastName}";

        public string StartsAtDateText =>
            Capitalize(LocalStartsAt.ToString("dddd, d. MMMM yyyy.", BosnianCulture));

        public string TimeRangeText =>
            DisplayFormat.TimeRange(LocalStartsAt, LocalEndsAt);

        public string TotalDurationText =>
            DisplayFormat.Duration(TotalDurationMinutes);

        public string TotalPriceText =>
            DisplayFormat.Price(TotalPrice);

        public string ServiceCountText =>
            DisplayFormat.ServiceCount(Appointments.Count);

        public bool IsCancelled =>
            Status == BookingStatus.Cancelled;

        public string TimeRange =>
            $"{LocalStartsAt:HH:mm} - {LocalEndsAt:HH:mm}";

        public string StatusName =>
            Status switch
            {
                BookingStatus.Scheduled => "Zakazano",
                BookingStatus.Completed => "Završeno",
                BookingStatus.Cancelled => "Otkazano",
                _ => "Nepoznato"
            };

        public string DurationText =>
            $"{TotalDurationMinutes} min";

        public string PriceText =>
            $"{TotalPrice:0.00} KM";

        public string CreatedAtText =>
            LocalCreatedAt.ToString("d. MMMM yyyy. HH:mm", BosnianCulture);

        public string CompletedAtText =>
            LocalCompletedAt.HasValue
                ? LocalCompletedAt.Value.ToString("d. MMMM yyyy. HH:mm", BosnianCulture)
                : "Nije završeno";

        public bool HasNotes =>
            !string.IsNullOrWhiteSpace(Notes);

        private static string Capitalize(string value) =>
            string.IsNullOrEmpty(value)
                ? value
                : char.ToUpper(value[0], BosnianCulture) + value[1..];
    }
}
