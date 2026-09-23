using AppointmentBooking.Mobile.Services.Api;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AppointmentBooking.Mobile.Services.ErrorHandling
{
    public sealed class ErrorHandler(ILogger<ErrorHandler> logger) : IErrorHandler
    {
        public async Task HandleAsync(Exception exception)
        {
            logger.LogError(
                exception,
                "An unhandled application error occurred.");

            string message = GetUserMessage(exception);

            await MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.DisplayAlert(
                    "Greška",
                    message,
                    "U redu"));
        }

        private static string GetUserMessage(Exception exception)
        {
            if (exception is ApiException apiException)
            {
                return apiException.StatusCode switch
                {
                    HttpStatusCode.BadRequest =>
                        "Podaci koje ste poslali nisu ispravni.",

                    HttpStatusCode.Unauthorized =>
                        "Vaša sesija je istekla. Prijavite se ponovo.",

                    HttpStatusCode.Forbidden =>
                        "Nemate dozvolu za ovu radnju.",

                    HttpStatusCode.NotFound =>
                        "Traženi podatak nije pronađen.",

                    HttpStatusCode.Conflict =>
                        "Radnja nije moguća jer je došlo do konflikta.",

                    HttpStatusCode.UnprocessableEntity =>
                        "Podaci nisu mogli biti obrađeni.",

                    HttpStatusCode.TooManyRequests =>
                        "Previše zahtjeva. Pokušajte ponovo za nekoliko trenutaka.",

                    HttpStatusCode.InternalServerError =>
                        "Došlo je do greške na serveru. Pokušajte ponovo.",

                    HttpStatusCode.BadGateway =>
                        "Server trenutno nije dostupan. Pokušajte ponovo.",

                    HttpStatusCode.ServiceUnavailable =>
                        "Usluga trenutno nije dostupna. Pokušajte ponovo.",

                    HttpStatusCode.GatewayTimeout =>
                        "Server nije odgovorio na vrijeme. Pokušajte ponovo.",

                    _ =>
                        "Došlo je do greške prilikom obrade zahtjeva."
                };
            }

            if (exception is HttpRequestException)
            {
                return "Nije moguće povezati se sa serverom. Provjerite internet vezu.";
            }

            if (exception is TaskCanceledException)
            {
                return "Zahtjev je istekao. Pokušajte ponovo.";
            }

            return "Došlo je do neočekivane greške. Pokušajte ponovo.";
        }
    }
}
