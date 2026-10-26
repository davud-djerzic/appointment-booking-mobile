using AppointmentBooking.Mobile.Models.Auth;
using AppointmentBooking.Mobile.Services.Authentication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Auth
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IAuthService authService;

        public RegisterViewModel(IAuthService authService)
        {
            this.authService = authService;
        }

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string phone = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

    [ObservableProperty]
    private bool isPasswordHidden = true;

    public string PasswordVisibilityIcon =>
        IsPasswordHidden ? "eye_hidden.svg" : "eye_open.svg";

        [ObservableProperty]
        private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool isConfirmPasswordHidden = true;

    public string ConfirmPasswordVisibilityIcon =>
        IsConfirmPasswordHidden ? "eye_hidden.svg" : "eye_open.svg";

        [ObservableProperty]
        private bool isBusy;

    [RelayCommand]
    private void TogglePasswordVisibility() =>
        IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility() =>
        IsConfirmPasswordHidden = !IsConfirmPasswordHidden;

    partial void OnIsPasswordHiddenChanged(bool value) =>
        OnPropertyChanged(nameof(PasswordVisibilityIcon));

    partial void OnIsConfirmPasswordHiddenChanged(bool value) =>
        OnPropertyChanged(nameof(ConfirmPasswordVisibilityIcon));

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (IsBusy) return;

            string firstName = FirstName.Trim();
            string lastName = LastName.Trim();
            string email = Email.Trim();
            string phone = Phone.Trim();

            if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Registracija",
                    "Popunite sva polja.",
                    "U redu");

                return;
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Registracija",
                    "Lozinke se ne podudaraju.",
                    "U redu");

                return;
            }

            IsBusy = true;

            try
            {
                RegisterRequest request = new(
                    firstName,
                    lastName,
                    email,
                    phone,
                    Password);

                AuthResult result =
                    await authService.RegisterAsync(request);

                if (!result.IsSuccess)
                {
                    await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                        "Registracija nije uspjela",
                        result.ErrorMessage
                            ?? "Registracija nije uspjela.",
                        "U redu");

                    return;
                }

                ClearFields();

                // SessionService has already stored the tokens
                // and changed the authentication state.
                // AppShell will switch to Home automatically.
            }
            catch (HttpRequestException)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Greška mreže",
                    "Nije moguće povezati se sa serverom.",
                    "U redu");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearFields()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }

        [RelayCommand]
        private async Task BackToLoginAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
