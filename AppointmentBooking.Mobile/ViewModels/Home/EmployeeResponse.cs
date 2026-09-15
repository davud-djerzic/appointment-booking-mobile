using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Home
{
    public sealed record EmployeeResponse(
        long Id,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
