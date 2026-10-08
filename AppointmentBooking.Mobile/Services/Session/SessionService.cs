using AppointmentBooking.Mobile.Models.Auth;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Storage;
using System.Text;
using System.Text.Json;

namespace AppointmentBooking.Mobile.Services.Session;

public sealed class SessionService(
    ITokenStorage tokenStorage,
    IAuthApiClient authApiClient) : ISessionService
{
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private string? accessToken;
    private DateTimeOffset? accessTokenExpiresAt;
    private string? role;

    public bool IsAuthenticated { get; private set; }

    public string? AccessToken => accessToken;

    public DateTimeOffset? AccessTokenExpiresAt =>
        accessTokenExpiresAt;

    public string? Role => role;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public async Task SignInAsync(
        AuthenticationTokenResponse token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        accessToken = token.AccessToken;
        accessTokenExpiresAt = token.AccessTokenExpiresAt;
        role = GetRoleFromAccessToken(token.AccessToken);

        await tokenStorage.SetRefreshTokenAsync(
            token.RefreshToken,
            token.RefreshTokenExpiresAt,
            cancellationToken);

        SetAuthenticationState(true);
    }

    public async Task<bool> RestoreSessionAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await RefreshAsync(cancellationToken);
    }

    public async Task<bool> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        await refreshLock.WaitAsync(cancellationToken);

        try
        {
            // Another request may already have refreshed the token
            // while this request was waiting for the lock.
            if (HasValidAccessToken())
            {
                SetAuthenticationState(true);
                return true;
            }

            string? refreshToken =
                await tokenStorage.GetRefreshTokenAsync(
                    cancellationToken);

            DateTimeOffset? refreshTokenExpiresAt =
                await tokenStorage.GetRefreshTokenExpiresAtAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                ClearSession();
                SetAuthenticationState(false);

                return false;
            }

            if (!refreshTokenExpiresAt.HasValue ||
                refreshTokenExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                await tokenStorage.ClearAsync(
                    cancellationToken);

                ClearSession();
                SetAuthenticationState(false);

                return false;
            }

            try
            {
                RefreshTokenRequest request =
                    new(refreshToken);

                AuthenticationTokenResponse? response =
                    await authApiClient.RefreshAsync(
                        request,
                        cancellationToken);

                if (response is null)
                {
                    return false;
                }

                accessToken = response.AccessToken;
                accessTokenExpiresAt =
                    response.AccessTokenExpiresAt;

                role = GetRoleFromAccessToken(
                    response.AccessToken);

                await tokenStorage.SetRefreshTokenAsync(
                    response.RefreshToken,
                    response.RefreshTokenExpiresAt,
                    cancellationToken);

                SetAuthenticationState(true);

                return true;
            }
            catch (ApiException ex)
                when (ex.StatusCode ==
                      System.Net.HttpStatusCode.Unauthorized)
            {
                await tokenStorage.ClearAsync(
                    cancellationToken);

                ClearSession();
                SetAuthenticationState(false);

                return false;
            }
            catch (ApiException)
            {
                // Temporary API/server problem.
                // Do not destroy the refresh token.
                ClearSession();
                SetAuthenticationState(false);

                return false;
            }
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task SignOutAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? refreshToken =
            await tokenStorage.GetRefreshTokenAsync(
                cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await authApiClient.LogoutAsync(
                    new RefreshTokenRequest(refreshToken),
                    cancellationToken);
            }
        }
        catch (ApiException)
        {
            // Local logout still happens.
        }
        finally
        {
            await tokenStorage.ClearAsync(
                cancellationToken);

            ClearSession();
            SetAuthenticationState(false);
        }
    }

    private bool HasValidAccessToken()
    {
        return !string.IsNullOrWhiteSpace(accessToken) &&
               accessTokenExpiresAt.HasValue &&
               accessTokenExpiresAt.Value > DateTimeOffset.UtcNow;
    }

    private void ClearSession()
    {
        accessToken = null;
        accessTokenExpiresAt = null;
        role = null;
    }

    private void SetAuthenticationState(
        bool isAuthenticated)
    {
        if (IsAuthenticated == isAuthenticated)
            return;

        IsAuthenticated = isAuthenticated;

        AuthenticationStateChanged?.Invoke(
            this,
            IsAuthenticated);
    }

    private static string? GetRoleFromAccessToken(
        string accessToken)
    {
        try
        {
            string[] parts = accessToken.Split('.');

            if (parts.Length != 3)
                return null;

            string payload = parts[1];

            int padding = payload.Length % 4;

            if (padding > 0)
            {
                payload = payload.PadRight(
                    payload.Length + (4 - padding),
                    '=');
            }

            payload = payload
                .Replace('-', '+')
                .Replace('_', '/');

            byte[] bytes =
                Convert.FromBase64String(payload);

            string json =
                Encoding.UTF8.GetString(bytes);

            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            if (root.TryGetProperty(
                    "role",
                    out JsonElement roleElement))
            {
                return roleElement.GetString();
            }

            const string roleClaim =
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

            if (root.TryGetProperty(
                    roleClaim,
                    out JsonElement roleClaimElement))
            {
                return roleClaimElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}