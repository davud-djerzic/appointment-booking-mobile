using AppointmentBooking.Mobile.Views.Shared;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace AppointmentBooking.Mobile.Services.Dialogs;

public static class AlertPanelService
{
    public static async Task DisplayAlertAsync(string title, string message, string accept)
    {
        Page? page = Shell.Current?.CurrentPage;
        if (page is null)
            return;

        await page.ShowPopupAsync(
            new AlertPanelPopup(title, message, accept),
            CreatePopupOptions());
    }

    public static async Task<bool> DisplayAlertAsync(
        string title,
        string message,
        string accept,
        string cancel)
    {
        Page? page = Shell.Current?.CurrentPage;
        if (page is null)
            return false;

        var result = await page.ShowPopupAsync<bool>(
            new AlertPanelPopup(title, message, accept, cancel),
            CreatePopupOptions());
        return result.Result;
    }

    private static PopupOptions CreatePopupOptions() => new()
    {
        PageOverlayColor = Color.FromArgb("#99000000"),
        Shape = new RoundRectangle
        {
            CornerRadius = 24,
            Fill = Color.FromArgb("#14264A"),
            Stroke = Colors.Transparent,
            StrokeThickness = 0
        }
    };
}
