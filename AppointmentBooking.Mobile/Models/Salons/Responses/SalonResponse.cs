using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Salons.Responses
{
    public sealed record SalonResponse(
        long Id,
        string Name,
        string Address,
        string City,
        string? InstagramUrl,
        string? FacebookUrl,
        decimal? Latitude,
        decimal? Longitude,
        IReadOnlyCollection<SalonWorkingHoursResponse> WorkingHours);
}
