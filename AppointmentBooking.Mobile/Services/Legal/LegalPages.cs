using AppointmentBooking.Mobile.Configuration;
using AppointmentBooking.Mobile.Services.Dialogs;

namespace AppointmentBooking.Mobile.Services.Legal
{
    /// <summary>
    /// Opens the public legal pages served by the API (wwwroot/legal).
    /// Both stores require the privacy policy to be reachable from inside the app.
    /// </summary>
    public static class LegalPages
    {
        private const string PrivacyPolicyPath = "privatnost";

        public static async Task OpenPrivacyPolicyAsync(ApiOptions apiOptions)
        {
            if (!Uri.TryCreate(apiOptions.BaseUrl, UriKind.Absolute, out Uri? baseUri) ||
                !Uri.TryCreate(baseUri, PrivacyPolicyPath, out Uri? privacyPolicyUri))
            {
                await ShowUnavailableAlertAsync();
                return;
            }

            try
            {
                await Browser.Default.OpenAsync(privacyPolicyUri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Politiku privatnosti nije moguće otvoriti: {ex}");
                await ShowUnavailableAlertAsync();
            }
        }

        private static Task ShowUnavailableAlertAsync() =>
            AlertPanelService.DisplayAlertAsync(
                "Politika privatnosti",
                "Nije moguće otvoriti Politiku privatnosti na ovom uređaju. Pokušajte ponovo ili provjerite da li je instaliran browser.",
                "U redu");
    }
}
