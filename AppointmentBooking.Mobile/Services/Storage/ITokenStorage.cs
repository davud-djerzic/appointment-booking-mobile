using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Storage
{
    public interface ITokenStorage
    {
        Task SetRefreshTokenAsync(string refreshToken, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

        Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);

        Task<DateTimeOffset?> GetRefreshTokenExpiresAtAsync(CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
