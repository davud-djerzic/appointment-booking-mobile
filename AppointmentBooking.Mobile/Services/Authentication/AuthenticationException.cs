using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Authentication
{
    public sealed class AuthenticationException : Exception
    {
        public AuthenticationException(string message)
            : base(message)
        {
        }
    }
}
