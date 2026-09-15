using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Auth
{
    public sealed record RegisterRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Password);
}
