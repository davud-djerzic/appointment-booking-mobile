using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Employees.Responses
{
    public sealed record BookableEmployeeResponse(
        long Id,
        string FirstName,
        string LastName);
}
