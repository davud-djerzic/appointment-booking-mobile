using AppointmentBooking.Mobile.Models.Auth;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Services.Storage;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AppointmentBooking.Mobile.Services.Authentication
{
    public sealed class AuthService(IApiClient apiClient, ISessionService sessionService) : IAuthService
    {
        public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                AuthenticationTokenResponse? response =
                    await apiClient.PostAsync<LoginRequest, AuthenticationTokenResponse>(
                        "api/auth/login",
                        request,
                        cancellationToken);

                if (response is null)
                {
                    return new AuthResult(
                        false,
                        null,
                        "Server je vratio prazan odgovor.");
                }

                await sessionService.SignInAsync(
                    response,
                    cancellationToken);

                return new AuthResult(
                    true,
                    response,
                    null);
            }
            catch (ApiException ex)
            {
                return ex.StatusCode switch
                {
                    HttpStatusCode.BadRequest =>
                        new AuthResult(
                            false,
                            null,
                            GetValidationMessage(ex.ResponseBody)),

                    HttpStatusCode.Unauthorized =>
                        new AuthResult(
                            false,
                            null,
                            "Email ili lozinka nisu ispravni."),

                    HttpStatusCode.InternalServerError =>
                        new AuthResult(
                            false,
                            null,
                            "Došlo je do greške na serveru. Pokušajte ponovo."),

                    _ =>
                        new AuthResult(
                            false,
                            null,
                            "Prijava trenutno nije moguća. Pokušajte ponovo.")
                };
            }
        }

        public async Task<AuthResult> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken = default)
        {
            try
            {
                AuthenticationTokenResponse? response =
                    await apiClient.PostAsync<RegisterRequest, AuthenticationTokenResponse>(
                        "api/auth/register",
                        request,
                        cancellationToken);

                if (response is null)
                {
                    return new AuthResult(
                        false,
                        null,
                        "Server je vratio prazan odgovor.");
                }

                await sessionService.SignInAsync(
                    response,
                    cancellationToken);

                return new AuthResult(
                    true,
                    response,
                    null);
            }
            catch (ApiException ex)
            {
                return ex.StatusCode switch
                {
                    HttpStatusCode.BadRequest =>
                        new AuthResult(
                            false,
                            null,
                            GetValidationMessage(ex.ResponseBody)),

                    HttpStatusCode.Conflict =>
                        new AuthResult(
                            false,
                            null,
                            "Korisnički račun sa ovim emailom već postoji."),

                    HttpStatusCode.InternalServerError =>
                        new AuthResult(
                            false,
                            null,
                            "Došlo je do greške na serveru. Pokušajte ponovo."),

                    _ =>
                        new AuthResult(
                            false,
                            null,
                            "Registracija trenutno nije moguća. Pokušajte ponovo.")
                };
            }
        }

        private static string GetValidationMessage(string responseBody)
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(responseBody);

                if (document.RootElement.TryGetProperty(
                        "errors",
                        out JsonElement errors))
                {
                    foreach (JsonProperty property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                            continue;

                        JsonElement firstError =
                            property.Value.EnumerateArray().FirstOrDefault();

                        if (firstError.ValueKind == JsonValueKind.String)
                        {
                            return firstError.GetString()
                                ?? "Podaci za prijavu nisu ispravni.";
                        }
                    }
                }
            }
            catch (JsonException)
            {
            }

            return "Podaci za prijavu nisu ispravni.";
        }
    }
}
