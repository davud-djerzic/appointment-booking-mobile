using Microsoft.Extensions.DependencyInjection;

namespace AppointmentBooking.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        InitializeComponent();

        this.services = services;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        AppShell appShell =
            services.GetRequiredService<AppShell>();

        return new Window(appShell);
    }
}