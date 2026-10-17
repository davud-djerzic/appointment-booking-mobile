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
    private bool isBusy;


    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync(
                "Prijava",
                "Unesite email i lozinku.",
                "OK");

            return;
        }

        LoginRequest request = new(
            Email.Trim(),
            Password);

        // Password više ne treba ostati u UI-u
        // nakon što je request napravljen.
        Password = string.Empty;

        IsBusy = true;

        try
        {
            // Task.Run ensures the HTTP call is off the Android main thread,
            // preventing Android.OS.NetworkOnMainThreadException.
            AuthResult result =
                await Task.Run(() => authService.LoginAsync(request));

            if (!result.IsSuccess)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Prijava nije uspjela",
                    result.ErrorMessage ?? "Prijava nije uspjela.",
                    "OK");

                return;
            }
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlertAsync(
                "Greška mreže",
                "Nije moguće povezati se sa serverom.",
                "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync(
                "Greška",
                "Prijava trenutno nije moguća. Pokušajte ponovo.",
                "OK");
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