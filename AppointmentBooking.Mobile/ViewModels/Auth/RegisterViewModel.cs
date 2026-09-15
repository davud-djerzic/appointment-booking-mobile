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
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isBusy;

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
                await Shell.Current.DisplayAlertAsync(
                    "Registracija",
                    "Popunite sva polja.",
                    "OK");

                return;
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Registracija",
                    "Lozinke se ne podudaraju.",
                    "OK");

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
                    await Shell.Current.DisplayAlertAsync(
                        "Registracija nije uspjela",
                        result.ErrorMessage
                            ?? "Registracija nije uspjela.",
                        "OK");

                    return;
                }

                // SessionService has already stored the tokens
                // and changed the authentication state.
                // AppShell will switch to Home automatically.
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Greška mreže",
                    "Nije moguće povezati se sa serverom.",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task BackToLoginAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
