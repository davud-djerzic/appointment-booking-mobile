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
        private bool isCurrentPasswordHidden = true;

        [ObservableProperty]
        private bool isNewPasswordHidden = true;

        [ObservableProperty]
        private bool isConfirmPasswordHidden = true;

        public string CurrentPasswordVisibilityIcon =>
            IsCurrentPasswordHidden ? "eye_hidden.svg" : "eye_open.svg";

        public string NewPasswordVisibilityIcon =>
            IsNewPasswordHidden ? "eye_hidden.svg" : "eye_open.svg";

        public string ConfirmPasswordVisibilityIcon =>
            IsConfirmPasswordHidden ? "eye_hidden.svg" : "eye_open.svg";

        [ObservableProperty]
        private bool isSaving;

        [RelayCommand]
        private void ToggleCurrentPasswordVisibility() =>
            IsCurrentPasswordHidden = !IsCurrentPasswordHidden;

        [RelayCommand]
        private void ToggleNewPasswordVisibility() =>
            IsNewPasswordHidden = !IsNewPasswordHidden;

        [RelayCommand]
        private void ToggleConfirmPasswordVisibility() =>
            IsConfirmPasswordHidden = !IsConfirmPasswordHidden;

        partial void OnIsCurrentPasswordHiddenChanged(bool value) =>
            OnPropertyChanged(nameof(CurrentPasswordVisibilityIcon));

        partial void OnIsNewPasswordHiddenChanged(bool value) =>
            OnPropertyChanged(nameof(NewPasswordVisibilityIcon));

        partial void OnIsConfirmPasswordHiddenChanged(bool value) =>
            OnPropertyChanged(nameof(ConfirmPasswordVisibilityIcon));

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
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Promjena lozinke",
                    "Molimo popunite sva polja.",
                    "U redu");

                return;
            }

            if (NewPassword.Length < 8 || NewPassword.Length > 100)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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

                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Lozinka promijenjena",
                    "Vaša lozinka je uspješno promijenjena.",
                    "U redu");

                // Pop the change-password page from the profile tab's navigation stack
                // before ending the session, so it cannot reappear on the next login.
                try
                {
                    await Shell.Current.GoToAsync("//main/profile-tab/profile");
                }
                catch (Exception navigationException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Nije moguće resetovati navigaciju profila nakon promjene lozinke: {navigationException}");
                }

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
