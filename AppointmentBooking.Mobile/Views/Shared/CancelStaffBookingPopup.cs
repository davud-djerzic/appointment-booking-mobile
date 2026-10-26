using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace AppointmentBooking.Mobile.Views.Shared;

public enum StaffBookingCancellationChoice
{
    CancelWithoutBreak,
    CancelAndCreateBreak
}

public sealed class CancelStaffBookingPopup : Popup<StaffBookingCancellationChoice?>
{
    public CancelStaffBookingPopup(string timeRange)
    {
        CanBeDismissedByTappingOutsideOfPopup = false;
        BackgroundColor = Colors.Transparent;
        Padding = 0;

        Button cancelWithBreak = CreateButton(
            "Otkaži i napravi pauzu",
            Color.FromArgb("#E7C45A"),
            Color.FromArgb("#07152F"));
        cancelWithBreak.Clicked += async (_, _) =>
            await CloseAsync(StaffBookingCancellationChoice.CancelAndCreateBreak);

        Button cancelOnly = CreateButton(
            "Otkaži bez pauze",
            Color.FromArgb("#07152F"),
            Colors.White,
            Color.FromArgb("#2B3D5C"));
        cancelOnly.Clicked += async (_, _) =>
            await CloseAsync(StaffBookingCancellationChoice.CancelWithoutBreak);

        Button back = CreateButton(
            "Nazad",
            Color.FromArgb("#07152F"),
            Colors.White,
            Color.FromArgb("#2B3D5C"));
        back.Clicked += async (_, _) => await CloseAsync(null);

        Content = new Border
        {
            WidthRequest = 340,
            MaximumWidthRequest = 420,
            Padding = 22,
            BackgroundColor = Color.FromArgb("#14264A"),
            Stroke = Color.FromArgb("#2B3D5C"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = "Otkazivanje termina",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = $"Termin {timeRange}. Odaberite šta želite uraditi.",
                        FontSize = 15,
                        TextColor = Colors.White,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    cancelWithBreak,
                    cancelOnly,
                    back
                }
            }
        };
    }

    private static Button CreateButton(string text, Color background, Color foreground, Color? border = null) => new()
    {
        Text = text,
        BackgroundColor = background,
        TextColor = foreground,
        BorderColor = border ?? background,
        BorderWidth = border is null ? 0 : 1,
        FontAttributes = FontAttributes.Bold,
        HeightRequest = 50,
        CornerRadius = 14
    };
}
