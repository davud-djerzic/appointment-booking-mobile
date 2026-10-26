using AppointmentBooking.Mobile.Models.Auth;
using AppointmentBooking.Mobile.Services.Authentication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppointmentBooking.Mobile.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService authService;

    public LoginViewModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isPasswordHidden = true;

    public string PasswordVisibilityIcon =>
        IsPasswordHidden ? "eye_hidden.svg" : "eye_open.svg";

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    partial void OnIsPasswordHiddenChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordVisibilityIcon));
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Prijava",
                "Unesite email i lozinku.",
                "U redu");

            return;
        }

        LoginRequest request = new(
            Email.Trim(),
            Password);

        IsBusy = true;

        try
        {
            // HTTP I/O is asynchronous; keep the UI responsive without
            // moving the request to a background thread.
            AuthResult result = await authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Prijava nije uspjela",
                    result.ErrorMessage ?? "Prijava nije uspjela.",
                    "U redu");

                return;
            }

            // Clear credentials only after authentication succeeds. Keeping
            // the password on failure lets the user correct or retry easily.
            Password = string.Empty;
        }
        catch (HttpRequestException)
        {
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Greška mreže",
                "Nije moguće povezati se sa serverom.",
                "U redu");
        }
        catch (Exception)
        {
            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Greška",
                "Prijava trenutno nije moguća. Pokušajte ponovo.",
                "U redu");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("register");
    }

    [RelayCommand]
    private async Task GoToForgotPasswordAsync() => await Shell.Current.GoToAsync("forgot-password");

    public void ClearFields()
    {
        Email = string.Empty;
        Password = string.Empty;
    }

}
