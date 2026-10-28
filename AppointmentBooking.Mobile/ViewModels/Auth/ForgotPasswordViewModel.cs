using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Authentication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Auth
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private const string AlertTitle = "Zaboravljena lozinka";
        private const int CodeLength = 6;

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

        // Buttons stay enabled while idle, so a tap on an empty or invalid field
        // explains what is missing instead of doing nothing.
        public bool IsNotBusy => !IsBusy;

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
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
            if (IsBusy)
                return;

            string email = Email.Trim();

            if (string.IsNullOrEmpty(email))
            {
                await ShowValidationAsync(
                    "Unesite email adresu na koju je registrovan vaš nalog.");

                return;
            }

            if (!IsValidEmail(email))
            {
                await ShowValidationAsync(
                    "Email adresa nije ispravna. Provjerite da li ste je dobro upisali, npr. korisnik@email.com.");

                return;
            }

            IsBusy = true;

            try
            {
                await authService.RequestPasswordResetAsync(
                    email);

                IsCodeStep = true;

                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Kod poslan",
                    "Poslali smo vam šestocifreni kod za potvrdu na email. Ako ga ne vidite u inboxu za nekoliko minuta, provjerite i folder Spam (Neželjena pošta).",
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

        [RelayCommand]
        private async Task VerifyCodeAsync()
        {
            if (IsBusy)
                return;

            string code = Code.Trim();

            if (string.IsNullOrEmpty(code))
            {
                await ShowValidationAsync(
                    "Unesite šestocifreni kod koji smo poslali na vaš email.");

                return;
            }

            if (code.Length != CodeLength || !code.All(char.IsAsciiDigit))
            {
                await ShowValidationAsync(
                    "Kod mora imati tačno 6 cifara. Provjerite kod u emailu.");

                return;
            }

            IsBusy = true;

            try
            {
                await authService.VerifyPasswordResetCodeAsync(
                    Email.Trim(),
                    code);

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

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                await ShowValidationAsync(
                    "Unesite novu lozinku.");

                return;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                await ShowValidationAsync(
                    "Ponovite novu lozinku u drugom polju.");

                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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

                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
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

        private static Task ShowValidationAsync(string message) =>
            AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                AlertTitle,
                message,
                "U redu");

        private static bool IsValidEmail(string email)
        {
            if (!MailAddress.TryCreate(email, out MailAddress? address) ||
                !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // MailAddress accepts "ime@domena"; a real address needs a dot in the domain.
            string host = address.Host;
            int lastDot = host.LastIndexOf('.');

            return lastDot > 0 && lastDot < host.Length - 1;
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

            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Promjena lozinke",
                message,
                "U redu");
        }
    }
}