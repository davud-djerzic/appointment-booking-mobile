using CommunityToolkit.Maui.Views;

namespace AppointmentBooking.Mobile.Views.Shared;

public partial class AlertPanelPopup : Popup<bool>
{
    // Closes only once, even if both buttons (or one button twice) are tapped quickly.
    private bool isClosing;

    public AlertPanelPopup(string title, string message, string accept, string? cancel = null)
    {
        CanBeDismissedByTappingOutsideOfPopup = false;
        BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#14264A");
        Padding = 0;

        Button acceptButton = CreateButton(accept, Microsoft.Maui.Graphics.Color.FromArgb("#E7C45A"), Microsoft.Maui.Graphics.Color.FromArgb("#07152F"), true);
        acceptButton.Clicked += OnAcceptClicked;

        VerticalStackLayout content = new()
        {
            Spacing = 16,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                },
                new Label
                {
                    Text = message,
                    FontSize = 15,
                    TextColor = Colors.White
                }
            }
        };

        if (cancel is not null)
        {
            Button cancelButton = CreateButton(cancel, Microsoft.Maui.Graphics.Color.FromArgb("#07152F"), Colors.White, false);
            cancelButton.Clicked += OnCancelClicked;
            content.Children.Add(new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 10,
                Children = { cancelButton, acceptButton }
            });
            Grid.SetColumn(acceptButton, 1);
        }
        else
        {
            content.Children.Add(acceptButton);
        }

        Content = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#14264A"),
            Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#2B3D5C"),
            StrokeThickness = 0.8,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            Padding = 22,
            WidthRequest = 340,
            Content = content
        };
    }

    private async void OnAcceptClicked(object? sender, EventArgs e) => await CloseOnceAsync(true);

    private async void OnCancelClicked(object? sender, EventArgs e) => await CloseOnceAsync(false);

    private async Task CloseOnceAsync(bool result)
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        await CloseAsync(result);
    }

    private static Button CreateButton(string text, Microsoft.Maui.Graphics.Color background, Microsoft.Maui.Graphics.Color foreground, bool bordered) => new()
    {
        Text = text,
        BackgroundColor = background,
        TextColor = foreground,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 14,
        HeightRequest = 50,
        BorderColor = bordered ? Microsoft.Maui.Graphics.Color.FromArgb("#E7C45A") : Microsoft.Maui.Graphics.Color.FromArgb("#2B3D5C"),
        BorderWidth = bordered ? 0 : 1
    };
}
