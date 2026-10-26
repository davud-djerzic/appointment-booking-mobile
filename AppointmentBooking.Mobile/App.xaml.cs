using AppointmentBooking.Mobile.Views.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentBooking.Mobile;

public partial class App : Application
{
    // Minimum time the startup panel with the salon logo is shown.
    private static readonly TimeSpan StartupPanelDuration = TimeSpan.FromSeconds(2);

    private readonly IServiceProvider services;
    private Task? startupTask;

    public App(IServiceProvider services)
    {
        InitializeComponent();

        this.services = services;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        // The window can be recreated by the OS after startup has already finished.
        if (startupTask is { IsCompleted: true })
        {
            return new Window(services.GetRequiredService<AppShell>());
        }

        // The startup panel is the window's own page, outside the Shell, so the Shell's
        // system-bar insets cannot move the logo while it is on screen.
        LoadingPage loadingPage = services.GetRequiredService<LoadingPage>();
        Window window = new(loadingPage);

        loadingPage.Loaded += OnLoadingPageLoaded;

        async void OnLoadingPageLoaded(object? sender, EventArgs e)
        {
            loadingPage.Loaded -= OnLoadingPageLoaded;
            startupTask ??= RunStartupAsync(window);
            await startupTask;
        }

        return window;
    }

    private async Task RunStartupAsync(Window window)
    {
        Task minimumDisplay = Task.Delay(StartupPanelDuration);

        AppShell appShell = services.GetRequiredService<AppShell>();
        await appShell.PrepareStartupAsync();

        await minimumDisplay;

        window.Page = appShell;
    }
}