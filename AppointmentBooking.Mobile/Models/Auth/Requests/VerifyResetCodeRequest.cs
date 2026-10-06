using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Auth.Requests
{
    public sealed record VerifyResetCodeRequest(
    string Email,
    string Code);
}
