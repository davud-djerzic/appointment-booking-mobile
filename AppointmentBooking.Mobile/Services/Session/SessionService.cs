using AppointmentBooking.Mobile.Models.Auth;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Storage;

namespace AppointmentBooking.Mobile.Services.Session;

public sealed class SessionService(
    ITokenStorage tokenStorage,
    IAuthApiClient authApiClient) : ISessionService
{
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private string? accessToken;
    private DateTimeOffset? accessTokenExpiresAt;

    public bool IsAuthenticated { get; private set; }

    public string? AccessToken => accessToken;

    public DateTimeOffset? AccessTokenExpiresAt =>
        accessTokenExpiresAt;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public async Task SignInAsync(
        AuthenticationTokenResponse token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        accessToken = token.AccessToken;
        accessTokenExpiresAt = token.AccessTokenExpiresAt;

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
                ClearAccessToken();
                SetAuthenticationState(false);

                return false;
            }

            if (!refreshTokenExpiresAt.HasValue ||
                refreshTokenExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                await tokenStorage.ClearAsync(
                    cancellationToken);

                ClearAccessToken();
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
                // The refresh token itself is no longer valid.
                await tokenStorage.ClearAsync(
                    cancellationToken);

                ClearAccessToken();
                SetAuthenticationState(false);

                return false;
            }
            catch (ApiException)
            {
                // Server error / temporary API problem.
                // Do not destroy a potentially valid refresh token.
                ClearAccessToken();
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

            ClearAccessToken();
            SetAuthenticationState(false);
        }
    }

    private bool HasValidAccessToken()
    {
        return !string.IsNullOrWhiteSpace(accessToken) &&
               accessTokenExpiresAt.HasValue &&
               accessTokenExpiresAt.Value > DateTimeOffset.UtcNow;
    }

    private void ClearAccessToken()
    {
        accessToken = null;
        accessTokenExpiresAt = null;
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
}