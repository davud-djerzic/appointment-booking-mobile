using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.Profile.Requests
{
    public sealed class ChangePasswordRequest
    {
        public required string CurrentPassword { get; init; }

        public required string NewPassword { get; init; }
    }
}
