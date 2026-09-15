using AppointmentBooking.Mobile.Models.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Authentication
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    }
}
