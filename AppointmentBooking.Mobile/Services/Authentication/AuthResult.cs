using AppointmentBooking.Mobile.Models.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Authentication
{
    public sealed record AuthResult(
        bool IsSuccess,
        AuthenticationTokenResponse? Token,
        string? ErrorMessage);
}
