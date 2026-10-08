using AppointmentBooking.Mobile.Models.Appointments.Responses;
using AppointmentBooking.Mobile.Models.Enums;

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
        public DateTimeOffset LocalStartsAt =>
            StartsAt.ToLocalTime();

        public DateTimeOffset LocalEndsAt =>
            EndsAt.ToLocalTime();

        public DateTimeOffset LocalCreatedAt =>
            CreatedAt.ToLocalTime();

        public DateTimeOffset? LocalCompletedAt =>
            CompletedAt?.ToLocalTime();

        public bool HasCompletedAt =>
            CompletedAt.HasValue;

        public string CustomerName =>
            $"{Customer.FirstName} {Customer.LastName}";

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
            LocalCreatedAt.ToString("d. MMMM yyyy. HH:mm");

        public string CompletedAtText =>
            LocalCompletedAt.HasValue
                ? LocalCompletedAt.Value.ToString("d. MMMM yyyy. HH:mm")
                : "Nije završeno";

        public bool HasNotes =>
            !string.IsNullOrWhiteSpace(Notes);
    }
}
