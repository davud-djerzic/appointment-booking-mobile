using AppointmentBooking.Mobile.Models.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace AppointmentBooking.Mobile.Services.Api
{
    public sealed class AuthApiClient(HttpClient httpClient) : IAuthApiClient
    {
        public async Task<AuthenticationTokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request,
                    cancellationToken);

            return await HandleResponseAsync<AuthenticationTokenResponse>(
                response,
                cancellationToken);
        }

        public async Task<AuthenticationTokenResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/auth/register",
                    request,
                    cancellationToken);

            return await HandleResponseAsync<AuthenticationTokenResponse>(
                response,
                cancellationToken);
        }

        public async Task<AuthenticationTokenResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/auth/refresh",
                    request,
                    cancellationToken);

            return await HandleResponseAsync<AuthenticationTokenResponse>(
                response,
                cancellationToken);
        }

        public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/auth/logout",
                    request,
                    cancellationToken);

            await HandleResponseAsync<object>(
                response,
                cancellationToken);
        }

        private static async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0)
                    return default;

                return await response.Content.ReadFromJsonAsync<T>(
                    cancellationToken);
            }

            string responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            throw new ApiException(
                response.StatusCode,
                responseBody);
        }
    }
}
