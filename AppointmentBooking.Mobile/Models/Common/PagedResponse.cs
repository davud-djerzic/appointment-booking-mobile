using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Common
{
    public sealed class PagedResponse<T>
    {
        public required IReadOnlyCollection<T> Items { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; init; }

        public int TotalPages { get; init; }
    }

}
