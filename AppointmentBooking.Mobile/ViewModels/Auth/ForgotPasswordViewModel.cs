using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Authentication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Auth
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private readonly IAuthService authService;

        public ForgotPasswordViewModel(IAuthService authService)
        {
            this.authService = authService;
        }

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string code = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isCodeStep;

        [ObservableProperty]
        private bool isPasswordStep;

        public bool IsEmailStep =>
            !IsCodeStep && !IsPasswordStep;

        public bool CanSubmitEmail =>
            !string.IsNullOrWhiteSpace(Email) &&
            !IsBusy;

        public bool CanSubmitCode =>
            !string.IsNullOrWhiteSpace(Code) &&
            Code.Length == 6 &&
            !IsBusy;

        public bool CanResetPassword =>
            !string.IsNullOrWhiteSpace(NewPassword) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            !IsBusy;

        partial void OnEmailChanged(string value)
        {
            OnPropertyChanged(nameof(CanSubmitEmail));
        }

        partial void OnCodeChanged(string value)
        {
            OnPropertyChanged(nameof(CanSubmitCode));
        }

        partial void OnNewPasswordChanged(string value)
        {
            OnPropertyChanged(nameof(CanResetPassword));
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            OnPropertyChanged(nameof(CanResetPassword));
        }

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSubmitEmail));
            OnPropertyChanged(nameof(CanSubmitCode));
            OnPropertyChanged(nameof(CanResetPassword));
        }

        partial void OnIsCodeStepChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEmailStep));
        }

        partial void OnIsPasswordStepChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEmailStep));
        }

        [RelayCommand]
        private async Task SendCodeAsync()
        {
            if (!CanSubmitEmail)
                return;

            IsBusy = true;

            try
            {
                await authService.RequestPasswordResetAsync(
                    Email.Trim());

                IsCodeStep = true;

                await Shell.Current.DisplayAlertAsync(
                    "Kod poslan",
                    "Provjerite email adresu. Poslali smo vam šestocifreni kod za potvrdu.",
                    "U redu");
            }
            catch (ApiException ex)
            {
                await ShowApiErrorAsync(
                    ex,
                    "Kod nije moguće poslati.");
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Greška mreže",
                    "Nije moguće povezati se sa serverom.",
                    "U redu");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task VerifyCodeAsync()
        {
            if (!CanSubmitCode)
                return;

            IsBusy = true;

            try
            {
                await authService.VerifyPasswordResetCodeAsync(
                    Email.Trim(),
                    Code.Trim());

                IsCodeStep = false;
                IsPasswordStep = true;
            }
            catch (ApiException ex)
            {
                await ShowApiErrorAsync(
                    ex,
                    "Kod nije ispravan.");
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Greška mreže",
                    "Nije moguće povezati se sa serverom.",
                    "U redu");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (!CanResetPassword)
                return;

            if (NewPassword != ConfirmPassword)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Nova lozinka",
                    "Lozinke se ne podudaraju.",
                    "U redu");

                return;
            }

            IsBusy = true;

            try
            {
                await authService.ResetPasswordAsync(
                    Email.Trim(),
                    Code.Trim(),
                    NewPassword,
                    ConfirmPassword);

                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                await Shell.Current.DisplayAlertAsync(
                    "Lozinka promijenjena",
                    "Vaša lozinka je uspješno promijenjena. Sada se možete prijaviti sa novom lozinkom.",
                    "U redu");

                await Shell.Current.GoToAsync("//login");
            }
            catch (ApiException ex)
            {
                await ShowApiErrorAsync(
                    ex,
                    "Lozinku nije moguće promijeniti.");
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Greška mreže",
                    "Nije moguće povezati se sa serverom.",
                    "U redu");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task BackToLoginAsync()
        {
            await Shell.Current.GoToAsync("//login");
        }

        [RelayCommand]
        private void BackToCode()
        {
            IsPasswordStep = false;
            IsCodeStep = true;

            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        [RelayCommand]
        private void BackToEmail()
        {
            IsPasswordStep = false;
            IsCodeStep = false;

            Code = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private static async Task ShowApiErrorAsync(
            ApiException ex,
            string fallbackMessage)
        {
            string message = ex.StatusCode switch
            {
                HttpStatusCode.BadRequest =>
                    "Podaci koje ste unijeli nisu ispravni.",

                HttpStatusCode.NotFound =>
                    "Podaci za promjenu lozinke nisu pronađeni.",

                HttpStatusCode.Conflict =>
                    "Zahtjev nije moguće obraditi. Pokušajte ponovo.",

                HttpStatusCode.TooManyRequests =>
                    "Previše pokušaja. Sačekajte malo pa pokušajte ponovo.",

                _ => fallbackMessage
            };

            await Shell.Current.DisplayAlertAsync(
                "Promjena lozinke",
                message,
                "U redu");
        }
    }
}