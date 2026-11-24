using AppointmentBooking.Mobile.Views.Shared;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace AppointmentBooking.Mobile.Services.Dialogs;

public static class AlertPanelService
{
    private static int openPopupCount;

    // True while an alert popup is on screen. A popup is shown as a page above the current one,
    // so a page can tell "covered by an alert" apart from "the user left the page"
    // (staff Termini must not jump back to the next working day after an alert).
    public static bool IsShowing => openPopupCount > 0;

    public static async Task DisplayAlertAsync(string title, string message, string accept)
    {
        Page? page = Shell.Current?.CurrentPage;
        if (page is null)
            return;

        openPopupCount++;
        try
        {
            await page.ShowPopupAsync(
                new AlertPanelPopup(title, message, accept),
                CreatePopupOptions());
        }
        finally
        {
            openPopupCount--;
        }
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

        openPopupCount++;
        try
        {
            var result = await page.ShowPopupAsync<bool>(
                new AlertPanelPopup(title, message, accept, cancel),
                CreatePopupOptions());
            return result.Result;
        }
        finally
        {
            openPopupCount--;
        }
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
