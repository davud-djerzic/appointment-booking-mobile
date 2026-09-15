using AppointmentBooking.Mobile.Models.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Session
{
    public interface ISessionService
    {
        bool IsAuthenticated { get; }

        string? AccessToken { get; }

        DateTimeOffset? AccessTokenExpiresAt { get; }

        event EventHandler<bool>? AuthenticationStateChanged;

        Task SignInAsync(AuthenticationTokenResponse token,CancellationToken cancellationToken = default);

        Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default);

        Task<bool> RefreshAsync(CancellationToken cancellationToken = default);

        Task SignOutAsync(CancellationToken cancellationToken = default);
    }
}
