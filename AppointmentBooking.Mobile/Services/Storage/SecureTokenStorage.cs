using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Storage
{
    public sealed class SecureTokenStorage : ITokenStorage
    {
        private const string RefreshTokenKey = "refresh_token";
        private const string RefreshTokenExpiresAtKey = "refresh_token_expires_at";

        public async Task SetRefreshTokenAsync(
            string refreshToken,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException(
                    "Refresh token cannot be empty.",
                    nameof(refreshToken));
            }

            await SecureStorage.Default.SetAsync(
                RefreshTokenKey,
                refreshToken);

            await SecureStorage.Default.SetAsync(
                RefreshTokenExpiresAtKey,
                expiresAt.ToString("O"));
        }

        public async Task<string?> GetRefreshTokenAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await SecureStorage.Default.GetAsync(
                RefreshTokenKey);
        }

        public async Task<DateTimeOffset?> GetRefreshTokenExpiresAtAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? value =
                await SecureStorage.Default.GetAsync(
                    RefreshTokenExpiresAtKey);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTimeOffset.TryParse(
                value,
                out DateTimeOffset expiresAt)
                    ? expiresAt
                    : null;
        }

        public Task ClearAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Remove current refresh-token data.
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(RefreshTokenExpiresAtKey);

            // Remove legacy access-token data created by the previous implementation.
            SecureStorage.Default.Remove("access_token");
            SecureStorage.Default.Remove("access_token_expires_at");

            return Task.CompletedTask;
        }
    }
}
