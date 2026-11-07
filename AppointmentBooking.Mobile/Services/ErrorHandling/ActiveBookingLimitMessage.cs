using AppointmentBooking.Mobile.Services.Api;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace AppointmentBooking.Mobile.Services.ErrorHandling;

/// <summary>
/// The API answers 409 "The customer already has the maximum of N upcoming bookings."
/// when the customer already has the allowed number of upcoming bookings.
/// </summary>
public static class ActiveBookingLimitMessage
{
    public const string Title = "Dostignut broj termina";

    public static bool IsLimitReached(ApiException exception) =>
        exception.StatusCode == HttpStatusCode.Conflict &&
        exception.ProblemDetail.Contains("upcoming bookings", StringComparison.OrdinalIgnoreCase);

    public static string Create(ApiException exception)
    {
        Match match = Regex.Match(exception.ProblemDetail, @"maximum of (\d+)", RegexOptions.CultureInvariant);

        string limit = match.Success &&
            int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                ? $" ({value})"
                : string.Empty;

        return $"Već imate najveći dozvoljeni broj zakazanih termina{limit}. " +
            "Otkažite neki od njih ili sačekajte da prođe, pa rezervišite novi termin.";
    }
}
