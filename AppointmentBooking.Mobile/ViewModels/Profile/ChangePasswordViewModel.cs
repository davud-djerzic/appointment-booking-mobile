using AppointmentBooking.Mobile.Models.Profile.Requests;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Services.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppointmentBooking.Mobile.ViewModels.Profile
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly IApiClient apiClient;
        private readonly IErrorHandler errorHandler;

        private readonly ISessionService sessionService;

        [ObservableProperty]
        private string currentPassword = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isSaving;

        public ChangePasswordViewModel(IApiClient apiClient, IErrorHandler errorHandler, ISessionService sessionService)
        {
            this.apiClient = apiClient;
            this.errorHandler = errorHandler;
            this.sessionService = sessionService;
        }

        [RelayCommand]
        private async Task ChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Promjena lozinke",
                    "Molimo popunite sva polja.",
                    "U redu");

                return;
            }

            if (NewPassword.Length < 8 || NewPassword.Length > 100)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Neispravna lozinka",
                    "Nova lozinka mora sadržavati između 8 i 100 znakova.",
                    "U redu");

                return;
            }

            if (!string.Equals(
                    NewPassword,
                    ConfirmPassword,
                    StringComparison.Ordinal))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Neispravna lozinka",
                    "Lozinke se ne podudaraju.",
                    "U redu");

                return;
            }

            if (string.Equals(
                    CurrentPassword,
                    NewPassword,
                    StringComparison.Ordinal))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Promjena lozinke",
                    "Nije moguće promijeniti lozinku. Pokušajte ponovo.",
                    "U redu");

                return;
            }

            ChangePasswordRequest request = new()
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword
            };

            try
            {
                IsSaving = true;

                await apiClient.PostAsync("api/profile/me/change-password", request);

                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                await Shell.Current.DisplayAlertAsync(
                    "Lozinka promijenjena",
                    "Vaša lozinka je uspješno promijenjena.",
                    "U redu");

                await sessionService.SignOutAsync();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private static async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}