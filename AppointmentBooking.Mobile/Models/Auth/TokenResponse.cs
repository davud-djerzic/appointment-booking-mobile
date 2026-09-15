using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Auth
{
    public sealed record TokenResponse(
        string AccessToken,
        DateTimeOffset ExpiresAt);
}
