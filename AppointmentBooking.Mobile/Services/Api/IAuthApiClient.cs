using AppointmentBooking.Mobile.Models.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Api
{
    public interface IAuthApiClient
    {
        Task<AuthenticationTokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<AuthenticationTokenResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        Task<AuthenticationTokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    }
}
