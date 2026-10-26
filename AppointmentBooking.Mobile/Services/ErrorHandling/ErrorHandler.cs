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
                "Došlo je do neobrađene greške u aplikaciji.");

            string title = exception is UserMessageException userMessage
                ? userMessage.Title
                : "Greška";

            string message = GetUserMessage(exception);

#if DEBUG
            // Debug builds only: show what actually failed, so errors like
            // "neočekivana greška" can be diagnosed on a device.
            if (exception is not UserMessageException)
            {
                message += $"\n\n[DEBUG] {exception.GetType().Name}: {exception.Message}";
            }
#endif

            await MainThread.InvokeOnMainThreadAsync(
                () => AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    title,
                    message,
                    "U redu"));
        }

        private static string GetUserMessage(Exception exception)
        {
            if (exception is UserMessageException userMessage)
            {
                return userMessage.Message;
            }

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
