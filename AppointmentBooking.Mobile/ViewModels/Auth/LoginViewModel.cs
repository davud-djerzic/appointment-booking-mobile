using AppointmentBooking.Mobile.Models.Auth;
using AppointmentBooking.Mobile.Services.Api;
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

        IsBusy = true;

        try
        {
            LoginRequest request = new(
                Email.Trim(),
                Password);

            AuthResult result =
                await authService.LoginAsync(request);

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
}