using AppointmentBooking.Mobile.Configuration;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Authentication;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using AppointmentBooking.Mobile.Services.Session;
using AppointmentBooking.Mobile.Services.Storage;
using AppointmentBooking.Mobile.ViewModels.Auth;
using AppointmentBooking.Mobile.ViewModels.Booking;
using AppointmentBooking.Mobile.ViewModels.Home;
using AppointmentBooking.Mobile.ViewModels.Profile;
using AppointmentBooking.Mobile.Views.Appointments;
using AppointmentBooking.Mobile.Views.Auth;
using AppointmentBooking.Mobile.Views.Booking;
using AppointmentBooking.Mobile.Views.Gallery;
using AppointmentBooking.Mobile.Views.Home;
using AppointmentBooking.Mobile.Views.Profile;
using AppointmentBooking.Mobile.Views.Startup;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Security;

namespace AppointmentBooking.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Pages / ViewModels
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LoginViewModel>();

        builder.Services.AddTransientWithShellRoute<
            RegisterPage,
            RegisterViewModel>("register");

        builder.Services.AddTransientWithShellRoute<
            ChangePasswordPage,
            ChangePasswordViewModel>("change-password");

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<BookingPage>();
        builder.Services.AddTransient<BookingViewModel>();
        builder.Services.AddTransient<AppointmentsPage>();
        builder.Services.AddTransient<GalleryPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<ProfileViewModel>();

#if DEBUG
        string apiBaseUrl =
            DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7236/"
                : "https://localhost:7236/";
#else
        string apiBaseUrl =
            throw new InvalidOperationException(
                "Production API URL has not been configured.");
#endif

        builder.Services.Configure<ApiOptions>(
            options =>
            {
                options.BaseUrl = apiBaseUrl;
            });

        // Storage
        builder.Services.AddSingleton<ITokenStorage, SecureTokenStorage>();

        // Authentication / Session
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // Authorization handler
        builder.Services.AddSingleton<BearerTokenHandler>();

        builder.Services.AddSingleton<IErrorHandler, ErrorHandler>();


        // Auth HTTP client
        builder.Services.AddSingleton<IAuthApiClient>(
            serviceProvider =>
            {
                ApiOptions options =
                    serviceProvider
                        .GetRequiredService<
                            IOptions<ApiOptions>>()
                        .Value;

                HttpClientHandler handler =
                    CreateHttpClientHandler();

                HttpClient httpClient =
                    new(handler)
                    {
                        BaseAddress =
                            new Uri(options.BaseUrl)
                    };

                return new AuthApiClient(httpClient);
            });

        // Protected API HTTP client
        builder.Services.AddSingleton<IApiClient>(
            serviceProvider =>
            {
                ApiOptions options =
                    serviceProvider
                        .GetRequiredService<
                            IOptions<ApiOptions>>()
                        .Value;

                HttpClientHandler innerHandler =
                    CreateHttpClientHandler();

                BearerTokenHandler authHandler =
                    serviceProvider
                        .GetRequiredService<
                            BearerTokenHandler>();

                authHandler.InnerHandler = innerHandler;

                HttpClient httpClient =
                    new(authHandler)
                    {
                        BaseAddress =
                            new Uri(options.BaseUrl)
                    };

                return new ApiClient(httpClient);
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static HttpClientHandler CreateHttpClientHandler()
    {
        HttpClientHandler handler = new();

#if DEBUG
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            handler.ServerCertificateCustomValidationCallback =
                (_, certificate, _, errors) =>
                {
                    if (certificate is not null &&
                        certificate.Issuer.Contains(
                            "CN=localhost",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return errors == SslPolicyErrors.None;
                };
        }
#endif

        return handler;
    }
}