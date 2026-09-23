using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.BookableServices.Responses
{
    public sealed class ServiceResponse
    {
        public long Id { get; init; }

        public required string Name { get; init; }

        public string? Description { get; init; }

        public required int DurationMinutes { get; init; }

        public required decimal Price { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
